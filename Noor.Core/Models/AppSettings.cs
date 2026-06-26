using System.Text.Json;
using System.Text.Json.Serialization;
using Noor.Models;

namespace Noor.Models;

public class AppSettings
{
    private static string? _settingsPath;

    public static string SettingsPath
    {
        get
        {
            if (_settingsPath == null)
            {
                _settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Noor",
                    "settings.json");
            }
            return _settingsPath;
        }
    }

    public Coordinates? Location { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Madhab Madhab { get; set; } = Madhab.Hanafi;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CalculationMethod CalculationMethod { get; set; } = CalculationMethod.MuslimWorldLeague;

    public int BlockDurationMinutes { get; set; } = 20;

    public string? AzanFilePath { get; set; }

    public int Volume { get; set; } = 80;

    public bool PlayAzan { get; set; } = true;

    public bool EnableNotifications { get; set; } = true;

    public bool ShowBlockScreen { get; set; } = true;

    public int NotificationMinutesBefore { get; set; } = 5;

    public string AccentColor { get; set; } = "#10B981";

    public bool Use24HourFormat { get; set; } = true;

    public bool IsDarkMode { get; set; } = false;

    public bool StartMinimized { get; set; } = false;

    public bool MinimizeToTray { get; set; } = true;

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (Location == null || !Location.IsValid)
            errors.Add("A valid location is required.");

        if (BlockDurationMinutes < 1 || BlockDurationMinutes > 120)
            errors.Add("Block duration must be between 1 and 120 minutes.");

        if (Volume < 0 || Volume > 100)
            errors.Add("Volume must be between 0 and 100.");

        if (NotificationMinutesBefore < 0)
            errors.Add("Notification minutes before cannot be negative.");

        if (!string.IsNullOrWhiteSpace(AzanFilePath) && !File.Exists(AzanFilePath))
            errors.Add($"Azan audio file not found: {AzanFilePath}");

        return errors;
    }

    public async Task SaveAsync(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(this, options);
        await File.WriteAllTextAsync(filePath, json);
    }

    public static async Task<AppSettings> LoadAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return new AppSettings();

        var json = await File.ReadAllTextAsync(filePath);
        var settings = JsonSerializer.Deserialize<AppSettings>(json);
        return settings ?? new AppSettings();
    }
}

/// <summary>
/// Prayer calculation method.
/// </summary>
public enum CalculationMethod
{
    /// <summary>
    /// Muslim World League (MWL)
    /// </summary>
    MuslimWorldLeague,

    /// <summary>
    /// Islamic Society of North America (ISNA)
    /// </summary>
    ISNA,

    /// <summary>
    /// Egyptian General Authority of Survey
    /// </summary>
    Egyptian,

    /// <summary>
    /// Umm Al-Qura University, Makkah
    /// </summary>
    Makkah,

    /// <summary>
    /// University of Islamic Sciences, Karachi
    /// </summary>
    Karachi,

    /// <summary>
    /// Tehran Institute of Geophysics
    /// </summary>
    Tehran,

    /// <summary>
    /// Shia Ithna-Ashari (Leva Institute)
    /// </summary>
    Jafari,

    /// <summary>
    /// Gulf Region
    /// </summary>
    Gulf,

    /// <summary>
    /// Kuwait
    /// </summary>
    Kuwait,

    /// <summary>
    /// Qatar
    /// </summary>
    Qatar,

    /// <summary>
    /// Majlis Ugama Islam Singapura (Singapore)
    /// </summary>
    Singapore,

    /// <summary>
    /// Ministry of Religious Affairs and Wakfs, Turkey
    /// </summary>
    Turkey,

    /// <summary>
    /// Moonsighting Committee (global)
    /// </summary>
    MoonsightingCommittee,

    /// <summary>
    /// Dubai (experimental)
    /// </summary>
    Dubai,

    /// <summary>
    /// Custom method (user-defined angles)
    /// </summary>
    Custom
}
