using Services.Shared;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Services
{
    public class FileSearchService
    {
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly object pauseLock = new object();
        private bool isPaused = false;

        public async Task SearchAsync(string? rootDirectory, FileSearchCriteria? criteria)
        {
            if (String.IsNullOrEmpty(rootDirectory) || criteria is null) return;

            try
            {
                var cancellationToken = cancellationTokenSource.Token;

                await Task.Run(async () =>
                {
                    var options = new ParallelOptions
                    {
                        CancellationToken = cancellationToken,
                        MaxDegreeOfParallelism = Environment.ProcessorCount // Adjust this as needed
                    };

                    void ProcessFile(string filePath)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return;

                        lock (pauseLock)
                        {
                            while (isPaused)
                            {
                                Monitor.Wait(pauseLock);
                                if (cancellationToken.IsCancellationRequested)
                                    return; // Check for cancellation after resuming
                            }
                        }

                        var fileInfo = new FileInfo(filePath);

                        try
                        {
                            if ((criteria!.FileNamePattern == "*" && (criteria.MinFileSizeBytes != -1 && fileInfo.Length > criteria.MinFileSizeBytes)) ||
                                (criteria.MinFileSizeBytes == -1 && (criteria.FileNamePattern != "*" && fileInfo.Name.Contains(criteria.FileNamePattern))) ||
                                (criteria.MinFileSizeBytes != -1 && criteria.FileNamePattern != "*" && fileInfo.Name.Contains(criteria.FileNamePattern) && fileInfo.Length > criteria.MinFileSizeBytes))
                            {
                                var result = new FileSearchResult(filePath, fileInfo.FullName, fileInfo.Length);
                                OnFileFoundAsync(result).Wait(); // Ensure async processing completes before adding to the list
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log the exception and continue with the next file
                            Console.WriteLine($"An error occurred while processing file '{filePath}': {ex.Message}");
                        }
                    }

                    void ProcessDirectory(string directory)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return;

                        try
                        {
                            var directoryFiles = Directory.GetFiles(directory);
                            Parallel.ForEach(directoryFiles, options, ProcessFile);

                            Parallel.ForEach(Directory.GetDirectories(directory), options, subdirectory =>
                            {
                                ProcessDirectory(subdirectory);
                            });
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            Console.WriteLine($"Access to a directory was denied: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"An error occurred: {ex.Message}");
                        }
                    }

                    ProcessDirectory(rootDirectory);

                    await OnFileSearchEnd();

                    Stop();
                }, cancellationToken);
            }
            catch
            {
                // Handle the exception, e.g., log it or skip the directory.
            }
        }

        public void Pause()
        {
            lock (pauseLock)
            {
                isPaused = true;
            }
        }

        public void Resume()
        {
            lock (pauseLock)
            {
                isPaused = false;
                Monitor.PulseAll(pauseLock); // Signal all waiting threads to resume
            }
        }

        public void Stop()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose(); // Dispose the old token source
            cancellationTokenSource = new CancellationTokenSource(); // Create a new token source for restarts
        }

        private readonly ConcurrentBag<Func<FileSearchResult, Task>> fileFoundHandlers = new ConcurrentBag<Func<FileSearchResult, Task>>();
        private readonly ConcurrentBag<Func<Task>> fileSearchEndHandlers = new ConcurrentBag<Func<Task>>();

        public event Func<FileSearchResult, Task> FileFoundAsync
        {
            add => fileFoundHandlers.Add(value);
            remove => fileFoundHandlers.TryTake(out _);
        }

        public event Func<Task> FileSearchEnd
        {
            add => fileSearchEndHandlers.Add(value);
            remove => fileSearchEndHandlers.TryTake(out _);
        }

        protected virtual async Task OnFileFoundAsync(FileSearchResult result)
        {
            var tasks = fileFoundHandlers.Select(handler => handler(result));
            await Task.WhenAll(tasks);
        }

        public async Task OnFileSearchEnd()
        {
            var tasks = fileSearchEndHandlers.Select(handler => handler());
            await Task.WhenAll(tasks);
        }


    }
}
