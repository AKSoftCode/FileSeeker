using Services.Shared;

namespace Services;

public class NotificationService
{
    public void SubscribeToFileSearch(FileSearchService fileSearch)
    {
        fileSearch.FileFoundAsync += HandleFileFound;
    }

    public async Task HandleFileFound(FileSearchResult result)
    {
        await Task.Delay(0);
        Console.WriteLine($"File Found: {result.FileName}, Path: {result.FilePath}, Size: {result.FileSizeBytes} bytes");
        // You can implement your UI notification logic here.
    }
}
