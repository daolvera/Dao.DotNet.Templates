# dao-aspire-avalonia

A .NET template that scaffolds an **Avalonia desktop app** connected to an **ASP.NET Core API**, orchestrated by **.NET Aspire**.

## What it creates

```
MyApp/
├── MyApp.AppHost/          # Aspire orchestrator
├── MyApp.Desktop/          # Avalonia desktop app (net10.0-windows)
├── MyApp.Api/              # ASP.NET Core minimal API
├── MyApp.ServiceDefaults/  # Shared Aspire service defaults
└── MyApp.Shared/           # Shared models (WeatherForecast)
```

## Install

```bash
dotnet new install Dao.Templates
```

## Use

```bash
dotnet new dao-aspire-avalonia -n MyApp
```

## Architecture

- **Desktop** uses `IHost` (Microsoft.Extensions.Hosting) for full DI and configuration support before Avalonia starts.
- **`IApiService`** wraps a typed `HttpClient` for API communication.
- **`INavigationService`** provides MVVM-friendly page navigation via `MainWindowViewModel.CurrentPage`.
- **Aspire AppHost** orchestrates the API and injects `Api__BaseUrl` into the desktop process at run time.
- The `if (builder.ExecutionContext.IsRunMode)` guard ensures the desktop project is only started when running locally — not during publish/container builds.

## Running

```bash
cd MyApp
dotnet run --project MyApp.AppHost
```

Aspire starts the API, waits for it to be healthy, then launches the desktop app with the correct `Api__BaseUrl` injected.

## Standalone (without Aspire)

Set `Api:BaseUrl` in `MyApp.Desktop/appsettings.json`:

```json
{
  "Api": {
    "BaseUrl": "http://localhost:5044"
  }
}
```

> **Note:** When launched via AppHost, the `Api__BaseUrl` environment variable automatically overrides the `appsettings.json` value.
