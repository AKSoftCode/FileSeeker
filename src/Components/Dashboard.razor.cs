using Microsoft.AspNetCore.Components;
using Services;
using Services.Shared;
using System.Diagnostics;
using System.IO;

namespace Components
{
    public partial class Dashboard
    {
        [Inject]
        RefreshService? refreshService { get; set; }

        [Inject]
        FileSearchService? fileSearchService { get; set; }

        DriveInfo[] drives = DriveInfo.GetDrives();

        private string namePattern_ = "*";
        private string? NamePattern
        {
            get
            {
                return namePattern_;
            }
            set
            {
                if (value != namePattern_)
                {
                    namePattern_ = value!;
                    StopSearch(true);

                    StateHasChanged();
                }
            }
        }

        private string selectedDrive_ = "";
        private string? SelectedDrive
        {
            get
            {
                return selectedDrive_;
            }
            set
            {
                if(value != selectedDrive_)
                {
                    selectedDrive_ = value!;

                    StopSearch(true);

                    StateHasChanged();
                }
            }
        }

        private long size = -1;
        private long SizePattern
        {
            get
            {
                return size;
            }
            set
            {
                if (value != size)
                {
                    size = value!;
                    StopSearch(true);
                    StateHasChanged();
                }
            }
        }

        private List<FileSearchResult> FilesFound = new();

        private readonly object filesAddition = new object();

        private bool SearchStarted = false;

        private bool SearchActivated = false;

        protected override Task OnInitializedAsync()
        {
            fileSearchService!.FileFoundAsync += OnFilesFoundAsync;
            fileSearchService!.FileSearchEnd += OnFilesSearchEndAsync;
            refreshService!.RefreshRequested += async () => await InvokeAsync(() => StateHasChanged()); ;

            return base.OnInitializedAsync();
        }

        private async Task OnFilesSearchEndAsync()
        {
            await Task.Delay(0); 
            SearchStarted = false;
            SearchActivated = false;
            await refreshService!.CallRequestRefreshAsync();
        }

        async Task OpenDrive(string? location)
        {
            await Task.Delay(0);
            #pragma warning disable CA1416
            _ = Process.Start("explorer.exe", location!);
        }

        async Task OnFilesFoundAsync(FileSearchResult result)
        {
            string? path = "";
            lock(filesAddition)
            {
                FilesFound.Add(result);
                path = result?.fileInfo?.FullName ?? "";
            }

            await Task.Run(() =>
            {
                string rootDirectory = Directory.GetParent(path ?? "")!.FullName;

                var rootDirInfo = new DirectoryInfo(rootDirectory!);
            });

            await refreshService!.CallRequestRefreshAsync();
        }

        async Task Search()
        {
            lock (filesAddition)
            {
                FilesFound.Clear();
            }

            await refreshService!.CallRequestRefreshAsync();

            if (String.IsNullOrEmpty(SelectedDrive)) return;

            await Task.Run(() => fileSearchService!.SearchAsync(SelectedDrive, new FileSearchCriteria(NamePattern!, SizePattern! * 1024 * 1024)));
        }

        Task StopSearch(bool clearResult = false)
        {
            if (SearchActivated)
                fileSearchService!.Stop();

            SearchStarted = false;
            SearchActivated = false;

            if(clearResult)
            {
                lock (filesAddition)
                {
                    FilesFound.Clear();
                }
            }

            StateHasChanged();

            return Task.CompletedTask;
        }

        public async Task OnStartStopToggle(bool toggled)
        {
            // Because variable is not two-way bound, we need to update it ourself
            SearchActivated = toggled;

            if (!SearchActivated)
            {
                await Task.Run(async () => await fileSearchService!.Pause());
            }
            else
            {
                if(!SearchStarted)
                {
                    SearchStarted = true;
                    await Search();
                }
                else
                {
                    await Task.Run(async () => await fileSearchService!.Resume());
                }
            }

            await refreshService!.CallRequestRefreshAsync();
        }
    }
}