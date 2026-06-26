namespace Noor.Models;

/// <summary>
/// Represents geographical coordinates for prayer time calculation.
/// </summary>
/// <param name="Latitude">Latitude in decimal degrees (-90 to 90)</param>
/// <param name="Longitude">Longitude in decimal degrees (-180 to 180)</param>
/// <param name="City">City name (optional, for display purposes)</param>
/// <param name="Country">Country name (optional, for display purposes)</param>
/// <param name="TimeZoneOffset">Timezone offset from UTC in hours (e.g., 6 for UTC+6, -5 for UTC-5)</param>
public record Coordinates(
    double Latitude,
    double Longitude,
    string? City = null,
    string? Country = null,
    double TimeZoneOffset = 0
)
{
    /// <summary>
    /// Validates if the coordinates are within valid ranges.
    /// </summary>
    public bool IsValid => Latitude is >= -90 and <= 90 && Longitude is >= -180 and <= 180;

    /// <summary>
    /// Returns a display-friendly string representation of the location.
    /// </summary>
    public string DisplayString => !string.IsNullOrWhiteSpace(City)
        ? $"{City}{(Country != null ? $", {Country}" : "")}"
        : $"{Latitude:F4}, {Longitude:F4}";
}
