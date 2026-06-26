using System.Text.Json;
using FluentAssertions;
using Noor.Models;
using Xunit;

namespace Noor.Tests.Models;

public class AppSettingsTests
{
    [Fact]
    public void Defaults_AreSane()
    {
        var settings = new AppSettings();

        settings.Madhab.Should().Be(Madhab.Hanafi);
        settings.CalculationMethod.Should().Be(CalculationMethod.MuslimWorldLeague);
        settings.BlockDurationMinutes.Should().Be(20);
        settings.Volume.Should().Be(80);
        settings.PlayAzan.Should().BeTrue();
        settings.EnableNotifications.Should().BeTrue();
        settings.ShowBlockScreen.Should().BeTrue();
        settings.NotificationMinutesBefore.Should().Be(5);
        settings.Use24HourFormat.Should().BeTrue();
        settings.Location.Should().BeNull();
    }

    [Fact]
    public void Validate_RequiresValidLocation()
    {
        var settings = new AppSettings();
        var errors = settings.Validate();
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.Contains("location", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_RangesBlockDuration()
    {
        var settings = new AppSettings
        {
            Location = new Coordinates(21.4225, 39.8262, "Makkah", "SA", 3),
            BlockDurationMinutes = 0
        };
        settings.Validate().Should().NotBeEmpty();

        settings.BlockDurationMinutes = 121;
        settings.Validate().Should().NotBeEmpty();

        settings.BlockDurationMinutes = 20;
        settings.Validate().Should().BeEmpty();
    }

    [Fact]
    public void Validate_RangesVolume()
    {
        var settings = new AppSettings
        {
            Location = new Coordinates(21.4225, 39.8262, "Makkah", "SA", 3),
            Volume = -1
        };
        settings.Validate().Should().NotBeEmpty();

        settings.Volume = 101;
        settings.Validate().Should().NotBeEmpty();

        settings.Volume = 50;
        settings.Validate().Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAndLoad_RoundTripsSettings()
    {
        var path = Path.Combine(Path.GetTempPath(), $"noor-test-{Guid.NewGuid():N}.json");
        try
        {
            var original = new AppSettings
            {
                Location = new Coordinates(40.7128, -74.0060, "New York", "USA", -5),
                Madhab = Madhab.Shafi,
                CalculationMethod = CalculationMethod.ISNA,
                BlockDurationMinutes = 15,
                Use24HourFormat = false,
                IsDarkMode = true
            };

            await original.SaveAsync(path);
            var loaded = await AppSettings.LoadAsync(path);

            loaded.Should().NotBeNull();
            loaded!.Madhab.Should().Be(Madhab.Shafi);
            loaded.CalculationMethod.Should().Be(CalculationMethod.ISNA);
            loaded.BlockDurationMinutes.Should().Be(15);
            loaded.Use24HourFormat.Should().BeFalse();
            loaded.IsDarkMode.Should().BeTrue();
            loaded.Location!.Latitude.Should().Be(40.7128);
            loaded.Location.Longitude.Should().Be(-74.0060);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task Enums_SerializeAsStrings()
    {
        var path = Path.Combine(Path.GetTempPath(), $"noor-test-{Guid.NewGuid():N}.json");
        try
        {
            var settings = new AppSettings
            {
                Location = new Coordinates(0, 0),
                Madhab = Madhab.Shafi,
                CalculationMethod = CalculationMethod.Makkah
            };

            await settings.SaveAsync(path);
            var json = await File.ReadAllTextAsync(path);

            json.Should().Contain("\"Shafi\"");
            json.Should().Contain("\"Makkah\"");
            json.Should().NotContain("Madhab\":0").And.NotContain("CalculationMethod\":3");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadAsync_ReturnsDefaults_WhenFileMissing()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid():N}.json");
        var settings = await AppSettings.LoadAsync(missing);
        settings.Should().NotBeNull();
        settings.Madhab.Should().Be(Madhab.Hanafi);
    }

    [Fact]
    public void CalculationMethod_HasAllExpectedMethods()
    {
        var values = Enum.GetNames<CalculationMethod>();
        values.Should().Contain(new[]
        {
            "MuslimWorldLeague", "ISNA", "Egyptian", "Makkah", "Karachi",
            "Tehran", "Jafari", "Gulf", "Kuwait", "Qatar",
            "Singapore", "Turkey", "MoonsightingCommittee", "Dubai", "Custom"
        });
    }
}
