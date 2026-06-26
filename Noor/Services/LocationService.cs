using System.Text.Json;
using System.Text.Json.Serialization;
using Noor.Models;

namespace Noor.Services;

/// <summary>
/// Service for determining user location via IP geolocation.
/// Uses the free, HTTPS-only ipwho.is endpoint (no API key required).
/// </summary>
public sealed class LocationService : IDisposable
{
    private readonly HttpClient _httpClient;
    private const string IpApiUrl = "https://ipwho.is/";

    public LocationService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    /// <summary>
    /// Gets the current location based on IP address.
    /// </summary>
    /// <returns>Coordinates with city and country information, or null if unavailable.</returns>
    public async Task<Coordinates?> GetLocationFromIpAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(IpApiUrl);

            var result = JsonSerializer.Deserialize<IpWhoResponse>(response);

            if (result == null || !result.Success || result.Latitude == null || result.Longitude == null)
            {
                return null;
            }

            // ipwho.is returns the UTC offset in seconds; convert to hours.
            var timezoneOffset = (result.Timezone?.Offset ?? 0) / 3600.0;

            return new Coordinates(
                result.Latitude.Value,
                result.Longitude.Value,
                result.City,
                result.Country,
                timezoneOffset
            );
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Creates coordinates from latitude and longitude.
    /// </summary>
    public Coordinates CreateCoordinates(double latitude, double longitude, string? city = null, string? country = null, double timeZoneOffset = 0)
    {
        return new Coordinates(latitude, longitude, city, country, timeZoneOffset);
    }

    /// <summary>
    /// Validates if coordinates are within valid ranges.
    /// </summary>
    public bool IsValidCoordinates(double latitude, double longitude)
    {
        return latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

/// <summary>
/// Response model from ipwho.is.
/// </summary>
internal sealed record IpWhoResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("country")]
    public string? Country { get; init; }

    [JsonPropertyName("city")]
    public string? City { get; init; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; init; }

    [JsonPropertyName("timezone")]
    public IpWhoTimezone? Timezone { get; init; }
}

internal sealed record IpWhoTimezone
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>UTC offset in seconds.</summary>
    [JsonPropertyName("offset")]
    public int Offset { get; init; }

    [JsonPropertyName("utc")]
    public string? Utc { get; init; }
}
