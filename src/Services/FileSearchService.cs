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
        private SemaphoreSlim pauseSemaphore = new SemaphoreSlim(1, 1); // Initialize with count 1 to start as not paused

        public async Task SearchAsync(string? rootDirectory, FileSearchCriteria? criteria)
        {
            if (String.IsNullOrEmpty(rootDirectory) || criteria is null) return;

            try
            {
                var cancellationToken = cancellationTokenSource.Token;

                await Task.Run(async () =>
                {
                    async Task ProcessFileAsync(string filePath)
                    {
                        await pauseSemaphore.WaitAsync(); // Wait if paused

                        if (cancellationToken.IsCancellationRequested)
                            return;

                        try
                        {
                            var fileInfo = new FileInfo(filePath);

                            if (MeetsCriteria(criteria, fileInfo))
                            {
                                var result = new FileSearchResult(fileInfo);
                                await OnFileFoundAsync(result); // Ensure async processing completes before adding to the list
                            }
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            Console.WriteLine($"Access to directory '{filePath}' was denied: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"An error occurred while processing directory '{filePath}': {ex.Message}");
                        }
                        finally
                        {
                            pauseSemaphore.Release(); // Release the semaphore after processing the file
                        }
                    }

                    async Task ProcessDirectoryAsync(string directory)
                    {
                        try
                        {
                            var directoryFiles = Directory.GetFiles(directory);
                            foreach (var file in directoryFiles)
                            {
                                await ProcessFileAsync(file);
                            }

                            var subdirectories = Directory.GetDirectories(directory);
                            foreach (var subdirectory in subdirectories)
                            {
                                await ProcessDirectoryAsync(subdirectory);
                            }
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

                    await ProcessDirectoryAsync(rootDirectory);

                }, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            finally
            {
                await OnFileSearchEnd();
            }
        }

        private bool MeetsCriteria(FileSearchCriteria criteria, FileInfo fileInfo)
        {
            return (criteria.FileNamePattern == "*" || fileInfo.Name.Contains(criteria.FileNamePattern))
                && (criteria.MinFileSizeBytes == -1 || fileInfo.Length > criteria.MinFileSizeBytes);
        }

        public async Task Pause()
        {
            await pauseSemaphore.WaitAsync(); // Wait for the semaphore to be released (pause)
        }

        public Task Resume()
        {
            pauseSemaphore.Release(); // Release the semaphore to resume
            return Task.CompletedTask;
        }

        public void Stop()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = new CancellationTokenSource();
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
