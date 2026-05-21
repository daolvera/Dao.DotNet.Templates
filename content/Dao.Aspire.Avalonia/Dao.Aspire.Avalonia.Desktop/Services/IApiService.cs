using Dao.Aspire.Avalonia.Shared.Models;

namespace Dao.Aspire.Avalonia.Desktop.Services;

public interface IApiService
{
    Task<List<WeatherForecast>> GetWeatherForecastAsync(CancellationToken ct = default);
}
