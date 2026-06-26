namespace Noor.Models;

/// <summary>
/// Represents prayer times for a specific day.
/// </summary>
/// <param name="Date">The date for which these prayer times are calculated</param>
/// <param name="Fajr">Fajr (dawn) prayer time</param>
/// <param name="Sunrise">Sunrise time</param>
/// <param name="Dhuhr">Dhuhr (noon) prayer time</param>
/// <param name="Asr">Asr (afternoon) prayer time</param>
/// <param name="Maghrib">Maghrib (sunset) prayer time</param>
/// <param name="Isha">Isha (night) prayer time</param>
public record PrayerTimes(
    DateOnly Date,
    TimeSpan Fajr,
    TimeSpan Sunrise,
    TimeSpan Dhuhr,
    TimeSpan Asr,
    TimeSpan Maghrib,
    TimeSpan Isha
)
{
    /// <summary>
    /// Gets all five daily prayer times as a dictionary.
    /// </summary>
    public IReadOnlyDictionary<PrayerType, TimeSpan> DailyPrayers => new Dictionary<PrayerType, TimeSpan>
    {
        [PrayerType.Fajr] = Fajr,
        [PrayerType.Dhuhr] = Dhuhr,
        [PrayerType.Asr] = Asr,
        [PrayerType.Maghrib] = Maghrib,
        [PrayerType.Isha] = Isha
    };

    /// <summary>
    /// Gets the time for a specific prayer type.
    /// </summary>
    public TimeSpan GetPrayerTime(PrayerType prayer) => prayer switch
    {
        PrayerType.Fajr => Fajr,
        PrayerType.Dhuhr => Dhuhr,
        PrayerType.Asr => Asr,
        PrayerType.Maghrib => Maghrib,
        PrayerType.Isha => Isha,
        _ => throw new ArgumentException($"Invalid prayer type: {prayer}", nameof(prayer))
    };
}

/// <summary>
/// Represents the type of Islamic prayer.
/// </summary>
public enum PrayerType
{
    /// <summary>
    /// Fajr - Dawn prayer, before sunrise
    /// </summary>
    Fajr,

    /// <summary>
    /// Dhuhr - Noon prayer, after the sun passes its zenith
    /// </summary>
    Dhuhr,

    /// <summary>
    /// Asr - Afternoon prayer
    /// </summary>
    Asr,

    /// <summary>
    /// Maghrib - Sunset prayer
    /// </summary>
    Maghrib,

    /// <summary>
    /// Isha - Night prayer
    /// </summary>
    Isha
}

/// <summary>
/// Extension methods for PrayerType.
/// </summary>
public static class PrayerTypeExtensions
{
    /// <summary>
    /// Gets the display name for the prayer type.
    /// </summary>
    public static string GetDisplayName(this PrayerType prayer) => prayer switch
    {
        PrayerType.Fajr => "Fajr",
        PrayerType.Dhuhr => "Dhuhr",
        PrayerType.Asr => "Asr",
        PrayerType.Maghrib => "Maghrib",
        PrayerType.Isha => "Isha",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets the Arabic name for the prayer type.
    /// </summary>
    public static string GetArabicName(this PrayerType prayer) => prayer switch
    {
        PrayerType.Fajr => "الفجر",
        PrayerType.Dhuhr => "الظهر",
        PrayerType.Asr => "العصر",
        PrayerType.Maghrib => "المغرب",
        PrayerType.Isha => "العشاء",
        _ => "غير معروف"
    };
}
