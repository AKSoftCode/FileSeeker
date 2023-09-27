using Microsoft.Extensions.DependencyInjection;

namespace Services;

public static class ServiceCollectionExtension
{
    public static void AddFileSeekerServices(this IServiceCollection services)
    {
        services.AddSingleton<RefreshService>();
        services.AddSingleton<FileSearchService>();
    }

}
