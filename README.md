# Noor

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10-purple.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Build](https://github.com/azimmahmud/Noor.Uno/actions/workflows/build.yml/badge.svg)](https://github.com/azimmahmud/Noor.Uno/actions/workflows/build.yml)
[![Uno Platform](https://img.shields.io/badge/Uno%20Platform-6.5-teal.svg)](https://platform.uno)

A beautiful Islamic prayer time reminder app built with **Uno Platform** and **.NET 10**. Features accurate prayer time calculations, Hijri calendar, azan audio, full-screen block screen, and a stunning Islamic-inspired UI.

![Light Theme](docs/screenshots/light-theme.png)
![Dark Theme](docs/screenshots/dark-theme.png)

## Features

### Prayer Times
- **Accurate calculations** using the Adhan library (supports all major calculation methods)
- **Real-time countdown** to the next prayer
- **Bilingual display** — English and Arabic prayer names
- **24-hour / 12-hour** time format toggle
- **5 daily prayers**: Fajr, Dhuhr, Asr, Maghrib, Isha

### Block Screen
- **Full-screen overlay** during prayer time with a 5-minute countdown
- **Azan audio playback** (NAudio, cross-platform)
- Dismissible after countdown completes

### Hijri Calendar
- **Full Hijri calendar** with month navigation
- **Islamic events** — Ashura, Mawlid, Isra & Mi'raj, Ramadan, Eid al-Fitr, Eid al-Adha, and more
- **Date converter** — Gregorian ↔ Hijri with Year/Month/Day dropdowns
- **Reminders** — add personal or religious reminders with category, repeat, and notification options

### Themes & UI
- **Light and Dark modes** with system-follow on first launch
- **Day-cycle inspired prayer colors** — vivid on light, muted on dark
- **Islamic geometric patterns** and crescent/mosque SVG decorations
- **Smooth animations** on calendar navigation, theme transitions, and card interactions

### Settings
- **Location** — auto-detect via IP or manual coordinate entry
- **Calculation method** — MuslimWorldLeague, ISNA, Egypt, Karachi, Tehran, Jafari, and more
- **Madhab** — Hanafi or Shafi
- **Block duration** — configurable 1–15 minutes
- **Custom azan audio** — choose your own MP3

## Screenshots

> Add your screenshots to `docs/screenshots/` and update the paths above.

| Light Theme | Dark Theme | Calendar |
|:-----------:|:----------:|:--------:|
| ![Light](docs/screenshots/light-theme.png) | ![Dark](docs/screenshots/dark-theme.png) | ![Calendar](docs/screenshots/calendar.png) |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Build & Run

```bash
# Restore packages
dotnet restore

# Build
dotnet build

# Run
dotnet run --project Noor
```

### Publish

```bash
# Windows (self-contained)
dotnet publish Noor -c Release -r win-x64 --self-contained -o ./publish/windows

# Linux (self-contained)
dotnet publish Noor -c Release -r linux-x64 --self-contained -o ./publish/linux
```

## Supported Platforms

| Platform | Status | Build Command |
|----------|--------|---------------|
| **Windows x64** | ✅ Supported | `dotnet build -f net10.0-desktop` |
| **Linux x64** | ✅ Supported | `dotnet build -f net10.0-desktop` |
| **Android** | 🔧 Configured | `dotnet build -f net10.0-android` |
| **iOS** | 🔧 Configured | `dotnet build -f net10.0-ios` (requires Mac) |
| **macOS** | 🔧 Configured | `dotnet build -f net10.0-maccatalyst` |

## Project Structure

```
Noor.Uno/
├── Noor/              # Main app project
│   ├── Assets/                 # SVG icons, splash screen
│   ├── Models/                 # Data models (AppSettings, PrayerTimes)
│   ├── Services/               # Business logic
│   │   ├── PrayerCalculatorService.cs   # Adhan wrapper
│   │   ├── HijriDateService.cs          # Gregorian ↔ Hijri
│   │   ├── AudioService.cs              # NAudio azan playback
│   │   ├── SchedulerService.cs          # Quartz.NET scheduling
│   │   ├── LocationService.cs           # IP geolocation
│   │   └── NotificationService.cs       # Desktop notifications
│   ├── Styles/                 # XAML resource dictionaries
│   │   ├── Colors.xaml          # Light/Dark theme colors
│   │   ├── Controls.xaml        # Button, card, input styles
│   │   └── TextStyles.xaml      # Typography definitions
│   ├── ViewModels/             # MVVM view models
│   └── Views/                  # XAML pages
│       ├── MainPage.xaml        # Dashboard with prayer times
│       ├── SettingsPage.xaml    # App configuration
│       ├── BlockScreenOverlay.xaml  # Full-screen prayer overlay
│       └── HijriCalendarPage.xaml   # Calendar + converter + reminders
├── .github/                    # CI, issue templates
├── CLAUDE.md                   # Detailed architecture docs
└── README.md
```

## Tech Stack

- **[Uno Platform 6.5](https://platform.uno)** — Cross-platform UI with WinUI/Skia
- **[.NET 10](https://dotnet.microsoft.com)** — Runtime and SDK
- **[Adhan](https://github.com/batoulapps/adhan-csharp)** — Islamic prayer time calculation
- **[NAudio](https://github.com/naudio/NAudio)** — Audio playback
- **[Quartz.NET](https://www.quartz-scheduler.net)** — Background job scheduling
- **[CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm)** — MVVM helpers

## Contributing

Contributions are welcome! Please read the [Contributing Guide](CONTRIBUTING.md) first.

- Use the [Bug Report](https://github.com/azimmahmud/Noor.Uno/issues/new?template=bug_report.yml) template for bugs
- Use the [Feature Request](https://github.com/azimmahmud/Noor.Uno/issues/new?template=feature_request.yml) template for ideas

## License

This project is licensed under the [MIT License](LICENSE).

## Acknowledgments

- [Adhan.NET](https://github.com/batoulapps/adhan-csharp) for accurate prayer time calculations
- [Uno Platform](https://platform.uno) for cross-platform UI capabilities
- The Islamic community for feedback and support
