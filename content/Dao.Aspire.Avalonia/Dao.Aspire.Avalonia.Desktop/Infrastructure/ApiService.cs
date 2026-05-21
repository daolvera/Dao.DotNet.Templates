using Dao.Aspire.Avalonia.Desktop.Services;
using Dao.Aspire.Avalonia.Shared.Models;
using System.Net.Http.Json;

namespace Dao.Aspire.Avalonia.Desktop.Infrastructure;

public class ApiService(HttpClient http) : IApiService
{
    public async Task<List<WeatherForecast>> GetWeatherForecastAsync(CancellationToken ct = default)
    {
        var result = await http.GetFromJsonAsync<List<WeatherForecast>>("api/weatherforecast", ct);
        return result ?? [];
    }
}
