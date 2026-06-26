# Noor — Project Context

> Single source of truth for architecture, conventions, and workflow.
> Read this fully before writing code, creating files, or running commands.

---

## Project Identity

| Field | Value |
|---|---|
| Solution | `Noor.sln` |
| Projects | `Noor.Core` (library), `Noor.App` (Avalonia UI), `Noor.Tests` |
| SDK | .NET 8 (`global.json` pins the SDK) |
| UI framework | Avalonia UI 11 (MVVM via CommunityToolkit.Mvvm) |
| Language | C# 12 (`ImplicitUsings`, `Nullable` enabled) |
| Primary targets | Windows, Linux, macOS (self-contained desktop) |
| Purpose | Islamic prayer-time reminder: accurate times, Hijri calendar, azan audio, focus overlay |

---

## Repository Structure

```
Noor/
├── Noor.sln
├── Directory.Build.props          # Nullable, ImplicitUsings, CPM
├── Directory.Packages.props       # Central Package Management
├── global.json
├── Noor.Core/                     # Pure domain logic (no UI deps)
│   ├── Models/
│   │   ├── AppSettings.cs         # user settings model + CalculationMethod enum + JSON persistence
│   │   ├── Coordinates.cs         # lat/long/city/country/timezoneOffset record
│   │   ├── Madhab.cs              # Shafi | Hanafi enum
│   │   └── PrayerTimes.cs         # prayer times record + PrayerType enum + Arabic/English names
│   ├── Services/
│   │   ├── PrayerCalculatorService.cs   # wraps the Adhan calculation library
│   │   ├── HijriDateService.cs          # static Gregorian↔Hijri (UmAlQuraCalendar) + events
│   │   ├── SchedulerService.cs          # Quartz.NET scheduling + PrayerJob
│   │   ├── LocationService.cs           # IP geolocation (ip-api.com)
│   │   ├── AudioService.cs              # LibVLCSharp azan playback
│   │   └── NotificationService.cs       # prayer/reminder notifications
│   └── Extensions/
├── Noor.App/                      # Avalonia UI project
│   ├── App.axaml(.cs)             # app lifecycle, DI container, theme
│   ├── ViewModels/                # ObservableObject view models
│   │   ├── MainViewModel.cs
│   │   └── SettingsViewModel.cs
│   ├── Views/                     # axaml windows/controls + code-behind
│   │   ├── MainWindow.axaml(.cs)
│   │   ├── SettingsView.axaml(.cs)
│   │   ├── HijriCalendarView.axaml(.cs)
│   │   └── BlockScreenOverlay.axaml(.cs)
│   ├── Assets/                    # icons, fonts, azan audio fallback
│   ├── Styles/                    # Colors.axaml, Controls.axaml, TextStyles.axaml
│   ├── Converters/
│   ├── appsettings.json           # logging config (NOT user settings)
│   └── Program.cs                 # Avalonia entry point
├── Noor.Tests/                    # xUnit + FluentAssertions
└── .github/                       # CI workflows, issue/PR templates, funding
```

---

## NuGet Dependencies (Central Package Management)

Managed in `Directory.Packages.props`. Do **not** add `Version=` to `<PackageReference />` in
`.csproj` files — add the version once in `Directory.Packages.props`.

| Package | Used by | Purpose |
|---|---|---|
| `Avalonia` | Noor.App | UI framework |
| `Avalonia.Desktop`, `Avalonia.Themes.Fluent` | Noor.App | Desktop target + Fluent theme |
| `Avalonia.Diagnostics` | Noor.App (Debug) | Dev overlay |
| `CommunityToolkit.Mvvm` | Noor.App | Source-generator MVVM (`ObservableProperty`, `RelayCommand`) |
| `Adhan` | Noor.Core | Accurate Islamic prayer-time calculation |
| `Quartz` | Noor.Core | Job scheduling (prayer alerts + daily recalculation) |
| `LibVLCSharp` + `VideoLAN.LibVLC.*` | Noor.Core | Azan audio playback (cross-platform) |
| `Microsoft.Extensions.DependencyInjection` | Noor.App | DI container |
| `Microsoft.Extensions.Http` | Noor.Core | `HttpClient` factory for geolocation |
| `xunit`, `xunit.runner.visualstudio` | Noor.Tests | Unit testing |
| `FluentAssertions` | Noor.Tests | Readable assertions |
| `Microsoft.NET.Test.Sdk` | Noor.Tests | Test host |

---

## Architecture Rules

### Separation of Concerns
- `Noor.Core` contains **all** business logic and must have **no dependency** on Avalonia or any UI
  namespace. It must be referenceable from `Noor.Tests` (a plain `net8.0` project) without pulling in UI.
- `Noor.App` contains only UI (views, view models, converters, styling). It references `Noor.Core`.
- View models live in `Noor.App/ViewModels` and depend on services from `Noor.Core` (injected).
- Code-behind (`*.axaml.cs`) must stay thin: delegate to the view model. No business logic in views.

### Dependency Injection
- The DI container is composed in `Noor.App/Program.cs` (or `App.axaml.cs`):
  - `Services` (prayer calculator, scheduler, location, audio, notification) → singleton or scoped.
  - `MainViewModel`, `SettingsViewModel` → transient.
- Services are injected into view models via constructor injection.
- Resolve the active `MainViewModel` from the service provider; do not `new` it up in views.

### Prayer Calculation
- Prayer times come from the **`Adhan` library** (`PrayerCalculatorService` wraps it). Do **not**
  hand-roll astronomical math — rely on Adhan for correctness.
- Default calculation method: `CalculationMethod.MuslimWorldLeague` (from `Noor.Core/Models/AppSettings.cs`).
- Default madhab: `Madhab.Hanafi`.
- When the user changes method/madhab/location, recompute via `PrayerCalculatorService` and refresh
  the scheduler.
- High-latitude fallback: when Adhan cannot compute a time (extreme latitudes), fall back to a sane
  default (e.g., Isha = 1.5h after Maghrib) and log a warning.

### Scheduling (Quartz.NET)
- `SchedulerService` owns a Quartz `IScheduler` plus per-prayer `System.Timers.Timer` instances.
- On `StartAsync`: calculate today's times, schedule each upcoming prayer, start a 60s poll timer
  that detects midnight to recalc + reschedule for the new day.
- `PrayerJob` is a Quartz `IJob`; the actual `PrayerTimeReached` event is raised by the service's
  timer (the job is a backup/persistence hook).
- Cancellation: each active prayer holds a `CancellationTokenSource`; `StopAsync` cancels all.

### Settings Persistence
- `AppSettings` is JSON-serialized via `System.Text.Json` with `WriteIndented = true`.
- Stored at: `Environment.SpecialFolder.LocalApplicationData/Noor/settings.json`
  (i.e. `%LOCALAPPDATA%\Noor\settings.json` on Windows, `~/.local/share/Noor/settings.json` on Linux).
- Loaded on startup, reloaded/saved on Settings changes. `Validate()` returns user-facing errors.
- Enums are serialized as strings (`JsonStringEnumConverter`).
- **Never** store secrets in `settings.json` — it is plain text.

### Location
- `LocationService` calls `http://ip-api.com/json/` (free, no API key) and maps the response to
  `Coordinates`. All network exceptions are caught → returns `null` → caller falls back to the last
  known location or the Makkah default.
- Users can override manually in Settings (lat/long/timezone/city/country).

### Block Screen / Focus Overlay
- `BlockScreenOverlay` is a full-screen transparent window shown above all content during prayer.
- A `System.Timers.Timer` ticks every second; the overlay auto-dismisses when the countdown hits 0
  (and is also manually dismissible).
- Countdown duration = `AppSettings.BlockDurationMinutes` (default 5, range 1–15).

### Audio
- `AudioService` uses **LibVLCSharp** with the native LibVLC binaries. `VideoLAN.LibVLC.*` packages
  provide the native libs per platform.
- Initialize one `LibVLC` instance (singleton) and reuse it; never spin up multiple media players.
- Default azan path is empty (no bundled audio). Users select a file in Settings; if absent, audio is skipped.

---

## Coding Conventions

### Naming
- Types: `PascalCase`. Interfaces: `I`-prefixed.
- Private fields: `_camelCase`.
- AXAML files match the code-behind class: `MainWindow.axaml` ↔ `MainWindow`.
- Namespaces match folders: `Noor.Core.Services`, `Noor.App.Views`, `Noor.App.ViewModels`.

### Async
- All I/O and scheduling methods are `async Task`. No `.Result` / `.Wait()`.
- UI event handlers may be `async void` only where Avalonia requires it.
- Timer callbacks (`System.Timers.Timer.Elapsed`) must marshal to the UI thread via
  `Dispatcher.UIThread.Post(...)` before touching observables bound to the UI.

### Error Handling
- Network/IO calls are wrapped in try/catch with typed exception handlers (no catch-all swallowing).
- Logging is via `ILogger<T>` (MS.Extensions.Logging) injected into services. Never swallow exceptions silently.

### Formatting & Style
- Enforced via `.editorconfig` and `dotnet format`. PRs must pass
  `dotnet format --verify-no-changes`.
- Use `DynamicResource` (not `StaticResource`) for theme-aware brushes in AXAML.

---

## App Startup Sequence (`Noor.App/Program.cs` → `App.axaml.cs`)
1. Build DI container (services + view models).
2. Configure logging from `appsettings.json`.
3. Create `MainWindow`, resolve `MainViewModel` as `DataContext`.
4. Load `AppSettings` from `settings.json` (creates defaults if missing).
5. Apply requested theme (dark/light) from settings.
6. `MainViewModel.OnLoaded` → resolve location (IP or default Makkah), start the scheduler, refresh the UI.

## Prayer Alert Sequence
1. Scheduler fires `PrayerTimeReached`.
2. `NotificationService.ShowPrayerNotification` plays azan (if enabled) and shows a notification.
3. If `ShowBlockScreen` is enabled, `MainWindow` shows `BlockScreenOverlay` for the configured duration.

---

## Build and Run Commands

```bash
dotnet restore
dotnet build Noor.sln
dotnet run --project Noor.App
dotnet test Noor.Tests
dotnet format Noor.sln --verify-no-changes

# Publish (self-contained single-file)
dotnet publish Noor.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o ./publish/windows
dotnet publish Noor.App -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o ./publish/linux
```

---

## What NOT to Do

- Do not add UI (Avalonia) namespaces to `Noor.Core`.
- Do not hand-roll prayer-time astronomy — use `Adhan`.
- Do not hardcode prayer times, coordinates, or timezone offsets in logic (defaults in models are OK).
- Do not use `Thread.Sleep` — use `Task.Delay` or Quartz triggers.
- Do not spin up multiple LibVLC/media-player instances simultaneously.
- Do not store secrets in `settings.json`.
- Do not use `.Result` / `.Wait()` — always `await`.

---

## Reference Docs

- Avalonia: https://docs.avaloniaui.net
- CommunityToolkit.Mvvm: https://learn.microsoft.com/dotnet/communitytoolkit/mvvm
- Adhan (prayer times): https://github.com/batoulapps/adhan-csharp
- Quartz.NET: https://www.quartz-scheduler.net/documentation
- LibVLCSharp: https://github.com/videolan/libvlcsharp
- ip-api.com: https://ip-api.com
