namespace Services;

public class RefreshService
{
    public event Func<Task>? RefreshRequested;

    public async Task CallRequestRefreshAsync()
    {
        if (RefreshRequested != null)
        {
            var handlers = RefreshRequested.GetInvocationList();

            foreach (var handler in handlers)
            {
                if (handler is Func<Task> asyncHandler)
                {
                    await asyncHandler();
                }
            }
        }
    }
}