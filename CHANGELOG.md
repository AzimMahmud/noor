# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- New `Noor.Core` class library (`net10.0`) hosting all UI-free domain logic: models, the prayer-time
  engine, Hijri calendar, scheduling, and location. This makes the core logic unit-testable.
- `Noor.Tests` xUnit project with **68 tests** covering the prayer calculator (ordering, every
  calculation method, a Makkah summer-solstice regression guard), Hijri service round-trips and events,
  `Coordinates` validation, and `AppSettings` validation/persistence.
- Cross-platform line-ending policy via `.gitattributes`.
- CI now runs the test suite (`dotnet test`) on Linux in addition to building Windows + Linux.
- **CodeQL** security analysis workflow (weekly + on PRs) and **Dependabot** config for NuGet + Actions.
- `CHANGELOG.md`, expanded `SECURITY.md` privacy section.

### Changed
- **Repository renamed to `Noor`** (was `Noor.Uno`); all README/CONTRIBUTING links and badges updated.
- Rewrote `README.md` and `CLAUDE.md` to accurately reflect the real Uno Platform stack. Removed
  incorrect claims (the app does **not** use the `Adhan` library or `CommunityToolkit.Mvvm`).
- Geolocation switched from `http://ip-api.com` (plain HTTP) to **`https://ipwho.is`** (HTTPS).
- Prayer-time engine now uses explicit morning/evening direction instead of angle-sign heuristics.

### Fixed
- **Sunrise, Maghrib, and Isha were computed incorrectly** because morning/evening direction was
  derived from the angle sign. Sunrise could land near 19:00 instead of ~05:35.
- **`Isha` used an angle of 90° for every calculation method** (meaningless). Methods now use correct
  Fajr/Isha angles, with Makkah/Gulf/Qatar using a fixed minutes-after-Maghrib Isha (PrayTimes-based).
- **`FormatDisplay` crashed** (`FormatException`) in 24-hour mode — `TimeSpan.ToString(@"HH\:mm")`
  is invalid; corrected to `@"hh\:mm"`.
- **Equation of time** produced spurious ~1-hour offsets due to independent mod-24 reduction; replaced
  with the validated NOAA/Spencer fractional-year approximation.
- Settings page discarded the selected calculation method (`Enum.TryParse` result was unused).
- Removed dead code (`CalculateSunPosition`, `FixHour`, unused `EarthRadius`, redundant `D` locals).

[Unreleased]: https://github.com/azimmahmud/Noor/compare/HEAD
