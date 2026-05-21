# dao-aspire-expo

A `dotnet new` template that scaffolds an Expo (React Native) mobile app connected to an ASP.NET Core Minimal API, orchestrated by .NET Aspire.

## What This Template Creates

```
MyApp/
├── MyApp.AppHost/          # .NET Aspire orchestration host
├── MyApp.Api/              # ASP.NET Core Minimal API (weather forecast endpoint)
├── MyApp.ServiceDefaults/  # Shared Aspire service defaults (telemetry, health checks)
├── MyApp.Mobile/           # Expo React Native app (Expo Router, TypeScript)
└── MyApp.slnx              # Solution file
```

## Installation & Usage

```bash
# Install the template pack
dotnet new install Dao.Templates

# Scaffold a new project
dotnet new dao-aspire-expo -n MyApp
```

## Architecture

- **AppHost** orchestrates all services via .NET Aspire's `DistributedApplication`.
- **API** exposes `/api/weatherforecast` and `/health` endpoints with CORS enabled for any origin (suitable for mobile dev).
- **Mobile** (Metro bundler) is added to the Aspire dashboard in `RunMode` via `AddJavaScriptApp`. In publish mode, only the API is deployed to the cloud.
- The mobile app reads `EXPO_PUBLIC_API_URL` at startup to know where the API lives. Aspire injects this automatically when running locally via `WithEnvironment`.

## ⚠️ Mobile API URL — Important

When running the app on a physical device or emulator, `localhost` refers to **the device itself**, not your development machine.

| Target | Use this URL |
|---|---|
| **iOS Simulator** | `http://localhost:<port>` (works fine) |
| **Android Emulator** | `http://10.0.2.2:<port>` (special loopback alias) |
| **Physical Device** | `http://<your-LAN-IP>:<port>` (e.g. `http://192.168.1.42:5000`) |

### Workarounds

**Option 1 — Override via `.env.local`** in `MyApp.Mobile/`:
```
EXPO_PUBLIC_API_URL=http://10.0.2.2:5000
```

**Option 2 — ADB reverse (Android Emulator only):**
```bash
adb reverse tcp:5000 tcp:5000
```
This forwards emulator port 5000 to your machine, so `localhost:5000` works inside the emulator.

**Option 3 — Update AppHost** to use your LAN IP:
```csharp
.WithEnvironment("EXPO_PUBLIC_API_URL", "http://192.168.1.42:5000")
```

## Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 10.0+ |
| Node.js | 20+ |
| Expo CLI | `npm install -g expo-cli` |
| Docker Desktop | Required if Aspire spins up containers |

## Running the App

```bash
cd MyApp
dotnet run --project MyApp.AppHost
```

This starts:
1. The **ASP.NET Core API** (visible in the Aspire dashboard)
2. The **Metro bundler** for the Expo app (also visible in dashboard)

Then in the Expo Metro output:
- Press `a` to open on Android emulator
- Press `i` to open on iOS simulator
- Scan the QR code with **Expo Go** to run on a physical device

## Project Structure Details

### `MyApp.Api`
- Minimal API with a `/api/weatherforecast` GET endpoint
- CORS configured to allow any origin (required for mobile clients on various IPs)
- OpenAPI/Swagger in Development
- `/health` and `/alive` endpoints via ServiceDefaults

### `MyApp.Mobile`
- Expo SDK 53 with Expo Router for file-based navigation
- TypeScript, strict mode
- `src/services/api.service.ts` — typed fetch wrapper that reads `EXPO_PUBLIC_API_URL`
- `app/index.tsx` — weather forecast screen
- `app/_layout.tsx` — root navigation stack

After scaffolding, install JS dependencies:
```bash
cd MyApp/MyApp.Mobile
npm install
```
