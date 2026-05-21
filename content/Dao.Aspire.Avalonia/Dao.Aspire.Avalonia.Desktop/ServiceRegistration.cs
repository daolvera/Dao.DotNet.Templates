using Dao.Aspire.Avalonia.Desktop.Infrastructure;
using Dao.Aspire.Avalonia.Desktop.Services;
using Dao.Aspire.Avalonia.Desktop.ViewModels;

namespace Dao.Aspire.Avalonia.Desktop;

public static class ServiceRegistration
{
    public static IServiceCollection AddDesktopServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["Api:BaseUrl"]
            ?? throw new InvalidOperationException(
                "Api:BaseUrl is not configured. " +
                "Run via AppHost (dotnet run --project *.AppHost) or set Api:BaseUrl in appsettings.json.");

        services.AddHttpClient<IApiService, ApiService>(client =>
        {
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + '/');
        });

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<INavigationService>(sp =>
            new NavigationService(sp.GetRequiredService<MainWindowViewModel>()));

        services.AddTransient<WeatherViewModel>();

        return services;
    }
}
