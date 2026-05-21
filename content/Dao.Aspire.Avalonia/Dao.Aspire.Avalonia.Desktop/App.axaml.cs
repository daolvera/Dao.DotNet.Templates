using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Dao.Aspire.Avalonia.Desktop.ViewModels;
using Dao.Aspire.Avalonia.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Dao.Aspire.Avalonia.Desktop;

public partial class App : global::Avalonia.Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Program.Services.GetRequiredService<MainWindowViewModel>()
            };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
