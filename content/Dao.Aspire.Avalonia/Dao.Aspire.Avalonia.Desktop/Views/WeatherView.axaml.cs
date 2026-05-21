using Avalonia.Controls;
using Dao.Aspire.Avalonia.Desktop.ViewModels;

namespace Dao.Aspire.Avalonia.Desktop.Views;

public partial class WeatherView : UserControl
{
    public WeatherView()
    {
        InitializeComponent();
        DataContextChanged += async (_, _) =>
        {
            if (DataContext is WeatherViewModel vm)
                await vm.LoadAsync();
        };
    }
}
