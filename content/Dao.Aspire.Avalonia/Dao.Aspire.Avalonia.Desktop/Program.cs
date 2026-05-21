using Avalonia;
using Microsoft.Extensions.Hosting;

namespace Dao.Aspire.Avalonia.Desktop;

sealed class Program
{
    private static IHost? _host;

    public static IServiceProvider Services => _host!.Services;

    [STAThread]
    public static void Main(string[] args)
    {
        _host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((_, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
                config.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddDesktopServices(ctx.Configuration);
            })
            .Build();

        _host.Start();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        _host.StopAsync().GetAwaiter().GetResult();
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
