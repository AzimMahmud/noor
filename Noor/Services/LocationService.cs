using System.Text.Json;
using System.Text.Json.Serialization;
using Noor.Models;

namespace Noor.Services;

/// <summary>
/// Service for determining user location via IP geolocation API.
/// </summary>
public class LocationService
{
    private readonly HttpClient _httpClient;
    private const string IpApiUrl = "http://ip-api.com/json/";

    public LocationService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    /// <summary>
    /// Gets the current location based on IP address using ip-api.com.
    /// </summary>
    /// <returns>Coordinates with city and country information</returns>
    public async Task<Coordinates?> GetLocationFromIpAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"{IpApiUrl}?fields=status,message,country,countryCode,city,lat,lon,timezone,offset");

            var result = JsonSerializer.Deserialize<IpApiResponse>(response);

            if (result == null || !result.Success || result.Lat == null || result.Lon == null)
            {
                return null;
            }

            // Convert offset from seconds to hours
            var timezoneOffset = (result.Offset ?? 0) / 3600.0;

            return new Coordinates(
                result.Lat.Value,
                result.Lon.Value,
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
    /// Validates if coordinates are valid.
    /// </summary>
    public bool IsValidCoordinates(double latitude, double longitude)
    {
        return latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180;
    }

    /// <summary>
    /// Disposes the HTTP client.
    /// </summary>
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

/// <summary>
/// Response model from ip-api.com.
/// </summary>
internal record IpApiResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("country")]
    public string? Country { get; init; }

    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; init; }

    [JsonPropertyName("city")]
    public string? City { get; init; }

    [JsonPropertyName("lat")]
    public double? Lat { get; init; }

    [JsonPropertyName("lon")]
    public double? Lon { get; init; }

    [JsonPropertyName("timezone")]
    public string? Timezone { get; init; }

    [JsonPropertyName("offset")]
    public double? Offset { get; init; }

    [JsonPropertyName("query")]
    public string? Ip { get; init; }

    [JsonIgnore]
    public bool Success => Status == "success";
}
