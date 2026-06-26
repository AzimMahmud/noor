using FluentAssertions;
using Noor.Models;
using Xunit;

namespace Noor.Tests.Models;

public class CoordinatesTests
{
    [Theory]
    [InlineData(21.4225, 39.8262, true)]   // Makkah
    [InlineData(-90, -180, true)]           // bounds
    [InlineData(90, 180, true)]             // bounds
    [InlineData(0, 0, true)]                // null island
    [InlineData(-90.1, 0, false)]           // lat too low
    [InlineData(90.1, 0, false)]            // lat too high
    [InlineData(0, -180.1, false)]          // lng too low
    [InlineData(0, 180.1, false)]           // lng too high
    public void IsValid_ValidatesRanges(double lat, double lng, bool expected)
    {
        var coords = new Coordinates(lat, lng);
        coords.IsValid.Should().Be(expected);
    }

    [Fact]
    public void DisplayString_UsesCityAndCountry_WhenAvailable()
    {
        var coords = new Coordinates(21.4225, 39.8262, "Makkah", "Saudi Arabia", 3.0);
        coords.DisplayString.Should().Be("Makkah, Saudi Arabia");
    }

    [Fact]
    public void DisplayString_FallsBackToCoordinates_WhenNoCity()
    {
        var coords = new Coordinates(21.4225, 39.8262);
        coords.DisplayString.Should().Contain("21.4225").And.Contain("39.8262");
    }

    [Fact]
    public void Records_WithSameValues_AreEqual()
    {
        var a = new Coordinates(1, 2, "X", "Y", 3);
        var b = new Coordinates(1, 2, "X", "Y", 3);
        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }
}
