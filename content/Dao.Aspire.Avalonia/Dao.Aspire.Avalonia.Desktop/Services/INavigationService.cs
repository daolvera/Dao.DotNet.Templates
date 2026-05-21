using Dao.Aspire.Avalonia.Desktop.ViewModels;

namespace Dao.Aspire.Avalonia.Desktop.Services;

public interface INavigationService
{
    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
}
