using CommunityToolkit.Mvvm.ComponentModel;

namespace Dao.Aspire.Avalonia.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase? _currentPage;

    public MainWindowViewModel(WeatherViewModel weatherViewModel)
    {
        CurrentPage = weatherViewModel;
    }
}
