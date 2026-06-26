# Noor — Project Context

> Single source of truth for architecture, conventions, and workflow.
> Read this fully before writing code, creating files, or running commands.

---

## Project Identity

| Field | Value |
|---|---|
| Solution | `Noor.sln` |
| App project | `Noor/Noor.csproj` (Uno Platform **single project**) |
| Test project | `Noor.Tests/Noor.Tests.csproj` (xUnit) |
| SDK | .NET 10 (`global.json` pins the SDK) |
| UI framework | Uno Platform 6.5 (WinUI API surface, Skia renderer) |
| Language | C# 14 (`ImplicitUsings`, `Nullable` enabled) |
| Pattern | MVVM with manual `INotifyPropertyChanged` |
| Primary targets | Windows (`net10.0-windows10.0.19041.0`), Linux (`net10.0-desktop`) |
| Scaffolded (opt-in) | Android, iOS, macOS Catalyst (uncomment in `Noor.csproj`) |
| Purpose | Islamic prayer-time reminder: accurate times, Hijri calendar, azan audio, focus overlay |

> **Note:** This is a **Uno Platform** app (WinUI API surface rendered by Skia), **not** Avalonia.
> If you previously saw references to Avalonia, `Noor.Core` / `Noor.App` / `Noor.Tests` projects,
> LibVLCSharp, or the `Adhan` NuGet package, those were outdated. The real stack is documented here.

---

## Repository Structure

```
Noor/
├── Noor.sln
├── Directory.Build.props        # Nullable, ImplicitUsings, CPM, NoWarn
├── Directory.Packages.props     # Central Package Management (NAudio, Quartz, MS.Extensions.Http)
├── global.json                  # Uno.Sdk version + SDK allowPrerelease:false
├── Noor/                        # Main Uno single-project app
│   ├── App.xaml(.cs)            # App class, merged resource dictionaries, navigation root
│   ├── GlobalUsings.cs          # project-wide global usings
│   ├── Models/
│   │   ├── AppSettings.cs       # user settings model + CalculationMethod enum + JSON persistence
│   │   ├── Coordinates.cs       # lat/long/city/country/timezoneOffset record
│   │   ├── Madhab.cs            # Shafi | Hanafi enum
│   │   └── PrayerTimes.cs       # prayer times record + PrayerType enum + Arabic/English names
│   ├── Services/                # business logic (NO UI dependency)
│   │   ├── PrayerCalculatorService.cs   # built-in astronomical prayer-time engine
│   │   ├── HijriDateService.cs          # static Gregorian↔Hijri (UmAlQuraCalendar) + events
│   │   ├── SchedulerService.cs          # Quartz.NET scheduling + PrayerJob
│   │   ├── LocationService.cs           # HTTPS IP geolocation (ipwho.is)
│   │   ├── AudioService.cs              # NAudio azan playback
│   │   └── NotificationService.cs       # prayer/reminder notifications
│   ├── ViewModels/
│   │   ├── MainPageViewModel.cs         # dashboard state + countdown
│   │   └── SettingsViewModel.cs
│   ├── Views/
│   │   ├── MainPage.xaml(.cs)           # dashboard
│   │   ├── SettingsPage.xaml(.cs)       # configuration
│   │   ├── HijriCalendarPage.xaml(.cs)  # calendar + converter + reminders
│   │   └── BlockScreenOverlay.xaml(.cs) # full-screen focus overlay
│   ├── Styles/                  # Colors.xaml, Controls.xaml, TextStyles.xaml
│   ├── Assets/                  # SVG icons, splash, geometric patterns
│   ├── Platforms/               # Desktop, Android, iOS, WebAssembly entry points
│   ├── Properties/              # launchSettings, publish profiles
│   └── Strings/                 # localized resources (en)
├── Noor.Tests/                  # xUnit + FluentAssertions
├── packaging/                   # installer assets + build scripts (Inno Setup, AppImage, .deb)
│   ├── icons/                   # generated raster icons (noor.ico, noor.png, sized PNGs)
│   ├── linux/                   # noor.desktop, build-appimage.sh, build-deb.sh
│   └── windows/                 # noor.iss (Inno Setup), build-installer.ps1
└── .github/                     # CI workflows, issue/PR templates, funding
```

---

## NuGet Dependencies (Central Package Management)

Managed in `Directory.Packages.props`. Do **not** add `Version=` to `<PackageReference />` in
`.csproj` files — add the version once in `Directory.Packages.props`.

| Package | Used by | Purpose |
|---|---|---|
| `Uno.Sdk` | Noor | UI framework, single-project model (version pinned in `global.json`) |
| `Quartz` | Noor | Job scheduling (prayer alerts + daily recalculation) |
| `NAudio` | Noor | Azan audio playback (**Windows-native**) |
| `Microsoft.Extensions.Http` | Noor | `HttpClient` factory support |
| `xunit`, `xunit.runner.visualstudio` | Noor.Tests | Unit testing |
| `FluentAssertions` | Noor.Tests | Readable assertions |
| `Microsoft.NET.Test.Sdk` | Noor.Tests | Test host |

---

## Architecture Rules

### Separation of Concerns
- `Noor/Services/` contains **all** business logic and must have **no dependency** on
  `Microsoft.UI.Xaml` or any view namespace.
- `Models/` are plain data types (records/enums) with no UI dependency.
- `ViewModels/` may reference Services and Models.
- `Views/` (`.xaml.cs`) reference ViewModels and Services; they own UI-thread marshalling
  via `DispatcherQueue.TryEnqueue(...)`.

### Prayer Calculation (IMPORTANT)
- Prayer times come from the **built-in astronomical engine** in `PrayerCalculatorService`.
- **Do not** introduce the `Adhan` NuGet package. The engine computes Julian Day, solar
  declination, equation of time, and applies per-method Fajr/Isha/Maghrib angles plus the
  Hanafi/Shafi Asr shadow factor. See the `#region Astronomical Calculations`.
- Default calculation method: `CalculationMethod.MuslimWorldLeague`.
- Default madhab: `Madhab.Hanafi`.
- High-latitude fallback: when Isha is non-computable, it defaults to ~1.5h after Maghrib.
- When changing the engine, update / add tests in `Noor.Tests` to lock in expected times for a
  known location (regression guard).

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
- `LocationService` calls `https://ipwho.is/` (free, HTTPS, no API key) and maps the response to
  `Coordinates`. All network exceptions are caught → returns `null` → caller falls back to the last
  known location or the Makkah default.
- Users can override manually in Settings (lat/long/timezone/city/country).

### Block Screen / Focus Overlay
- `BlockScreenOverlay` is a `UserControl` added to `MainPage.BlockScreenHost`.
- A `System.Timers.Timer` ticks every second; the overlay auto-dismisses when the countdown hits 0
  (and is also manually dismissible).
- Countdown duration = `AppSettings.BlockDurationMinutes` (default 20, range 1–120).

### Audio
- `AudioService` uses `NAudio.Wave.WaveOutEvent`. `LibVLC`/`WaveOut` from NAudio is **Windows-native**.
- On non-Windows desktops, audio needs a cross-platform backend (tracked as a roadmap item).
- The `LibVLC`/NAudio output is initialized lazily and reused; never spin up multiple outputs.
- Default azan path is empty (no bundled audio). Users select a file in Settings; if absent, audio is skipped.

---

## Coding Conventions

### Naming
- Types: `PascalCase`. Interfaces: `I`-prefixed.
- Private fields: `_camelCase`.
- XAML files match the code-behind class: `MainPage.xaml` ↔ `MainPage`.
- Namespaces match folders: `Noor.Services`, `Noor.Views`, `Noor.Models`, `Noor.ViewModels`.

### Async
- All I/O and scheduling methods are `async Task`. No `.Result` / `.Wait()`.
- UI event handlers may be `async void` only where Avalonia/WinUI requires it.
- Timer callbacks (`System.Timers.Timer.Elapsed`) must marshal to the UI thread via
  `DispatcherQueue.TryEnqueue(...)` before touching UI elements.

### Error Handling
- Network/IO calls are wrapped in try/catch with typed exception handlers (no catch-all swallowing).
- Logging is currently via `System.Diagnostics.Debug.WriteLine`; prefer structured logging where
  available. Never swallow exceptions silently.

### Formatting & Style
- Enforced via `.editorconfig` and `dotnet format`. PRs must pass
  `dotnet format --verify-no-changes`.
- Use `ThemeResource` (not `StaticResource`) for theme-aware brushes in XAML.

---

## App Startup Sequence (`App.OnLaunched`)
1. Create `MainWindow`, set icon.
2. Load `AppSettings` from `settings.json` (creates defaults if missing).
3. Apply requested theme (dark/light) from settings.
4. Navigate the root `Frame` to `Views.MainPage`.
5. `MainPage.OnLoaded` → `MainPageViewModel.InitializeAsync()`:
   - load settings, resolve location (IP or default Makkah), start the scheduler, refresh the UI.

## Prayer Alert Sequence
1. Scheduler fires `PrayerTimeReached`.
2. `NotificationService.ShowPrayerNotification` plays azan (if enabled) and shows a notification.
3. If `ShowBlockScreen` is enabled, `MainPage` shows `BlockScreenOverlay` for the configured duration.

---

## Build and Run Commands

```bash
dotnet restore
dotnet build Noor.sln -c Release
dotnet run --project Noor
dotnet test Noor.Tests
dotnet format Noor.sln --verify-no-changes

# Publish
dotnet publish Noor -c Release -f net10.0-windows10.0.19041.0 -r win-x64 --self-contained -o ./publish/windows
dotnet publish Noor -c Release -f net10.0-desktop -r linux-x64 --self-contained -o ./publish/linux

# Build installers (output → dist/)
./packaging/linux/build-appimage.sh        # → dist/Noor-<ver>-linux-x64.AppImage
./packaging/linux/build-deb.sh             # → dist/noor_<ver>-1_amd64.deb
pwsh packaging/windows/build-installer.ps1  # → dist/NoorSetup-<ver>-win-x64.exe (needs Inno Setup 6)
```

> In CI / fresh environments set `NUGET_PACKAGES` to a writable cache on a partition with free space
> (the Uno SDK restore is large).

---

## Packaging & Distribution

Installers are produced by the scripts in `packaging/` and built locally on each
platform (no release automation — artifacts are uploaded manually to GitHub Releases).

| Platform | Format | Script | Output |
|---|---|---|---|
| Windows | Inno Setup `.exe` | `packaging/windows/build-installer.ps1` | `dist/NoorSetup-<ver>-win-x64.exe` |
| Linux | AppImage (portable) | `packaging/linux/build-appimage.sh` | `dist/Noor-<ver>-linux-x64.AppImage` |
| Linux | `.deb` (Debian/Ubuntu) | `packaging/linux/build-deb.sh` | `dist/noor_<ver>-1_amd64.deb` |

- All builds are **self-contained** (no .NET runtime needed on the target machine); trimming is
  **off** by default for Skia/XAML reliability.
- The `.deb` installs the app to `/usr/lib/noor/` with a `/usr/bin/noor` launcher and hicolor icons.
- The AppImage bundles the full publish output in an `AppDir` with a shell `AppRun`.
- Version is read from `$NOOR_VERSION` (defaults to `1.0.0`).
- Raster icons in `packaging/icons/` are generated from `Noor/Assets/Icons/icon.svg`.

---

## What NOT to Do

- Do not add UI namespaces (`Microsoft.UI.Xaml`, etc.) to `Services/` or `Models/`.
- Do not hardcode prayer times, coordinates, or timezone offsets in logic (defaults in models are OK).
- Do not use `Thread.Sleep` — use `Task.Delay` or Quartz triggers.
- Do not spin up multiple audio outputs simultaneously.
- Do not introduce the `Adhan` package — use the built-in calculation engine.
- Do not store secrets in `settings.json`.
- Do not use `.Result` / `.Wait()` — always `await`.

---

## Reference Docs

- Uno Platform: https://docs.platform.uno
- Quartz.NET: https://www.quartz-scheduler.net/documentation
- NAudio: https://github.com/naudio/NAudio
- Umm al-Qura calendar (.NET): https://learn.microsoft.com/dotnet/api/system.globalization.umalquracalendar
- ipwho.is: https://ipwho.is
