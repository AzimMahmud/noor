# Contributing to Noor

Thank you for your interest in contributing! This document provides guidelines and information for contributors.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Windows**: Visual Studio 2022+ or Rider with WinUI workloads
- **Linux**: VS Code with C# Dev Kit or Rider
- **Git**: Latest version

## Getting Started

1. **Fork** the repository
2. **Clone** your fork:
   ```bash
   git clone https://github.com/YOUR_USERNAME/Noor.git
   cd Noor
   ```
3. **Build** the project:
   ```bash
   dotnet build
   ```
4. **Run** the app:
   ```bash
   dotnet run --project Noor
   ```
5. **Run the tests**:
   ```bash
   dotnet test
   ```

## Project Structure

```
Noor/
├── Noor/              # Main Uno app project (Views, ViewModels, Audio, Notifications, Styles)
├── Noor.Core/         # UI-free domain logic (Models, prayer calc, Hijri, scheduling, location)
├── Noor.Tests/        # xUnit + FluentAssertions unit tests
├── .github/           # CI workflows, issue templates, funding, Dependabot
├── CLAUDE.md          # Architecture & conventions (read this!)
└── README.md
```

## Development Workflow

1. Create a feature branch from `main`:
   ```bash
   git checkout -b feature/your-feature-name
   ```
2. Make your changes
3. Ensure the build passes:
   ```bash
   dotnet build
   ```
4. Format your code:
   ```bash
   dotnet format
   ```
5. Commit with a clear message
6. Push and open a Pull Request

## Code Style

- Follow C# conventions (PascalCase for public, `_camelCase` for private fields)
- Use `ThemeResource` not `StaticResource` for theme-aware brushes
- All I/O and scheduling methods are `async Task` — no `.Result` or `.Wait()`
- Keep `Noor.Core` free of UI dependencies (`Microsoft.UI.Xaml`, etc.)
- Wrap location/network calls in try/catch with typed handlers; log the error
- When changing the prayer calculation engine, add/update tests in `Noor.Tests` to lock in expected times

## Pull Request Guidelines

- One feature/fix per PR
- Describe what changed and why
- Include screenshots for UI changes
- Ensure `dotnet build` passes with 0 errors, 0 warnings
- Ensure `dotnet test` passes
- Keep PRs focused and small when possible

## Reporting Bugs

Use the [Bug Report template](https://github.com/azimmahmud/Noor/issues/new?template=bug_report.yml) and include:
- Steps to reproduce
- Expected vs actual behavior
- OS and .NET version

## Suggesting Features

Use the [Feature Request template](https://github.com/azimmahmud/Noor/issues/new?template=feature_request.yml).

## Code of Conduct

This project follows the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md). By participating, you agree to uphold it.

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
