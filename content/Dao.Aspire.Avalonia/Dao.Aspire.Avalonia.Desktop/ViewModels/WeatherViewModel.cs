using CommunityToolkit.Mvvm.ComponentModel;
using Dao.Aspire.Avalonia.Desktop.Services;
using Dao.Aspire.Avalonia.Shared.Models;

namespace Dao.Aspire.Avalonia.Desktop.ViewModels;

public partial class WeatherViewModel(IApiService apiService) : ViewModelBase
{
    [ObservableProperty]
    private List<WeatherForecast> _forecasts = [];

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string? _errorMessage;

    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            Forecasts = await apiService.GetWeatherForecastAsync(ct);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load forecasts: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
