using Dao.Aspire.Avalonia.Desktop.Services;
using Dao.Aspire.Avalonia.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Dao.Aspire.Avalonia.Desktop.Infrastructure;

public class NavigationService(MainWindowViewModel mainWindowViewModel) : INavigationService
{
    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        var vm = Program.Services.GetRequiredService<TViewModel>();
        mainWindowViewModel.CurrentPage = vm;
    }
}
