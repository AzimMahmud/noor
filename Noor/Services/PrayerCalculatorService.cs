using Noor.Models;

namespace Noor.Services;

/// <summary>
/// Service for calculating Islamic prayer times using astronomical calculations.
/// </summary>
public class PrayerCalculatorService
{
    /// <summary>
    /// Calculates prayer times for a specific date and location.
    /// </summary>
    public PrayerTimes CalculatePrayerTimes(
        DateOnly date,
        Coordinates coordinates,
        Madhab madhab = Madhab.Hanafi,
        CalculationMethod calculationMethod = CalculationMethod.MuslimWorldLeague)
    {
        var (lat, lng, tz) = (coordinates.Latitude, coordinates.Longitude, coordinates.TimeZoneOffset);
        var times = CalculateTimes(date, lat, lng, tz, calculationMethod);

        // Apply Asr Madhab adjustment
        times.Asr = CalculateAsr(date, lat, lng, tz, madhab);

        // Adjust for higher latitudes (Isha)
        times.Isha = AdjustIshaForHighLatitudes(times.Isha, times.Maghrib, lat);

        return new PrayerTimes(
            date,
            times.Fajr,
            times.Sunrise,
            times.Dhuhr,
            times.Asr,
            times.Maghrib,
            times.Isha
        );
    }

    /// <summary>
    /// Calculates prayer times for today.
    /// </summary>
    public PrayerTimes CalculateForToday(
        Coordinates coordinates,
        Madhab madhab = Madhab.Hanafi,
        CalculationMethod calculationMethod = CalculationMethod.MuslimWorldLeague)
    {
        return CalculatePrayerTimes(DateOnly.FromDateTime(DateTime.Today), coordinates, madhab, calculationMethod);
    }

    /// <summary>
    /// Calculates prayer times for tomorrow.
    /// </summary>
    public PrayerTimes CalculateForTomorrow(
        Coordinates coordinates,
        Madhab madhab = Madhab.Hanafi,
        CalculationMethod calculationMethod = CalculationMethod.MuslimWorldLeague)
    {
        return CalculatePrayerTimes(DateOnly.FromDateTime(DateTime.Today.AddDays(1)), coordinates, madhab, calculationMethod);
    }

    /// <summary>
    /// Calculates prayer times for the next N days.
    /// </summary>
    public IReadOnlyList<PrayerTimes> CalculateForDateRange(
        DateOnly startDate,
        int days,
        Coordinates coordinates,
        Madhab madhab = Madhab.Hanafi,
        CalculationMethod calculationMethod = CalculationMethod.MuslimWorldLeague)
    {
        var results = new List<PrayerTimes>(days);

        for (int i = 0; i < days; i++)
        {
            var date = startDate.AddDays(i);
            results.Add(CalculatePrayerTimes(date, coordinates, madhab, calculationMethod));
        }

        return results;
    }

    /// <summary>
    /// Determines the next prayer time from now given current prayer times.
    /// </summary>
    public (PrayerType prayerType, DateTime prayerTime, TimeSpan timeUntil)? GetNextPrayerFromTimes(
        PrayerTimes todayPrayers)
    {
        if (todayPrayers == null) return null;

        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);

        // Check each prayer time in order
        foreach ((PrayerType type, TimeSpan time) in todayPrayers.DailyPrayers.OrderBy(p => p.Value))
        {
            var prayerDateTime = today.ToDateTime(time);

            if (prayerDateTime > now)
            {
                return (type, prayerDateTime, prayerDateTime - now);
            }
        }

        // If we've passed all prayers today, return null (caller should recalculate)
        return null;
    }

    /// <summary>
    /// Gets the current prayer (if any) or null.
    /// </summary>
    public PrayerType? GetCurrentPrayer(
        PrayerTimes todayPrayers,
        int prayerDurationMinutes = 20)
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        var currentTime = now.TimeOfDay;

        // Check if we're within the duration of any prayer
        foreach ((PrayerType type, TimeSpan time) in todayPrayers.DailyPrayers)
        {
            var prayerStart = today.ToDateTime(time);
            var prayerEnd = prayerStart.AddMinutes(prayerDurationMinutes);

            if (now >= prayerStart && now < prayerEnd)
            {
                return type;
            }
        }

        return null;
    }

    #region Astronomical Calculations

    /// <summary>
    /// Computes the solar declination (degrees) and the equation of time (hours) for a date,
    /// using the Spencer (1971) / NOAA fractional-year approximation. Accurate to within
    /// ~0.01° declination and ~0.5 minute of the equation of time — more than sufficient for
    /// minute-granularity prayer times.
    /// </summary>
    private static (double declination, double equationOfTime) CalculateSunParameters(DateOnly date)
    {
        var n = date.DayOfYear;
        var gamma = 2.0 * Math.PI / 365.0 * (n - 1);

        // Solar declination in radians (Spencer 1971), converted to degrees.
        var declinationRad = 0.006918
                           - 0.399912 * Math.Cos(gamma)
                           + 0.070257 * Math.Sin(gamma)
                           - 0.006758 * Math.Cos(2 * gamma)
                           + 0.000907 * Math.Sin(2 * gamma)
                           - 0.002697 * Math.Cos(3 * gamma)
                           + 0.001480 * Math.Sin(3 * gamma);
        var declination = declinationRad * 180.0 / Math.PI;

        // Equation of time in minutes (NOAA), converted to hours.
        var eqMinutes = 229.18 * (0.000075
                        + 0.001868 * Math.Cos(gamma)
                        - 0.032077 * Math.Sin(gamma)
                        - 0.014615 * Math.Cos(2 * gamma)
                        - 0.040849 * Math.Sin(2 * gamma));
        var equationOfTime = eqMinutes / 60.0;

        return (declination, equationOfTime);
    }

    /// <summary>
    /// Normalizes an hour value to be between 0 and 24.
    /// </summary>
    private static double NormalizeHour(double hour)
    {
        hour = hour % 24;
        if (hour < 0) hour += 24;
        return hour;
    }

    private static TimeSpan ConvertHourToTimeSpan(double hour)
    {
        // Normalize the hour to be between 0 and 24 before converting
        hour = NormalizeHour(hour);

        var hours = (int)hour;
        var minutes = (int)((hour - hours) * 60);
        return new TimeSpan(hours, minutes, 0);
    }

    private static PrayerTimeTimes CalculateTimes(DateOnly date, double lat, double lng, double tz, CalculationMethod method)
    {
        var (declination, eqT) = CalculateSunParameters(date);
        var p = GetCalculationParameters(method);

        // Solar noon (base time for every sun-angle calculation): 12 + tz - lng/15 - eqT
        var noon = 12 + tz - lng / 15.0 - eqT;

        var times = new PrayerTimeTimes
        {
            // Morning prayers subtract the hour angle; evening prayers add it.
            Fajr = HourAngleTime(noon, p.FajrAngle, lat, declination, morning: true),
            Sunrise = HourAngleTime(noon, 0.833, lat, declination, morning: true),
            Dhuhr = ConvertHourToTimeSpan(NormalizeHour(noon)),
            Maghrib = AddMinutes(HourAngleTime(noon, 0.833, lat, declination, morning: false), p.MaghribMinutesAfterSunset),
            // Asr is assigned separately based on the madhab.
        };

        // Isha: angle-based for most methods; fixed minutes after Maghrib for others.
        if (p.IshaAngle > 0)
            times.Isha = HourAngleTime(noon, p.IshaAngle, lat, declination, morning: false);
        else
            times.Isha = AddMinutes(times.Maghrib, p.IshaMinutesAfterMaghrib);

        return times;
    }

    private static TimeSpan CalculateAsr(DateOnly date, double lat, double lng, double tz, Madhab madhab)
    {
        var (declination, eqT) = CalculateSunParameters(date);
        var noon = 12 + tz - lng / 15.0 - eqT;

        // Shafi: shadow length = 1 × object; Hanafi: shadow length = 2 × object.
        var shadowFactor = (madhab == Madhab.Hanafi) ? 2.0 : 1.0;

        // Sun altitude at which the object's shadow equals shadowFactor × its height.
        var altDiffRad = Math.Abs(lat - declination) * Math.PI / 180.0;
        var altitudeDeg = -Math.Atan(1.0 / (shadowFactor + Math.Tan(altDiffRad))) * 180.0 / Math.PI;

        // Asr is always in the afternoon.
        return HourAngleTime(noon, altitudeDeg, lat, declination, morning: false);
    }

    /// <summary>
    /// Computes a prayer time from the sun's hour angle for a given depression angle.
    /// Morning prayers (Fajr, Sunrise) subtract the hour angle; evening prayers (Maghrib, Isha) add it.
    /// </summary>
    private static TimeSpan HourAngleTime(double noonHour, double angleDeg, double lat, double declination, bool morning)
    {
        var latRad = lat * Math.PI / 180.0;
        var declRad = declination * Math.PI / 180.0;
        var angleRad = angleDeg * Math.PI / 180.0;

        var num = -Math.Sin(angleRad) - Math.Sin(latRad) * Math.Sin(declRad);
        var den = Math.Cos(latRad) * Math.Cos(declRad);

        if (Math.Abs(den) < 0.0001)
            return TimeSpan.Zero;

        var cosH = Math.Clamp(num / den, -1.0, 1.0);
        var hourAngle = Math.Acos(cosH) * 180.0 / Math.PI / 15.0;

        var time = morning ? noonHour - hourAngle : noonHour + hourAngle;
        return ConvertHourToTimeSpan(NormalizeHour(time));
    }

    private static TimeSpan AddMinutes(TimeSpan baseTime, int minutes)
    {
        if (minutes == 0) return baseTime;
        return ConvertHourToTimeSpan(NormalizeHour(baseTime.TotalHours + minutes / 60.0));
    }

    private static CalculationParameters GetCalculationParameters(CalculationMethod method)
    {
        return method switch
        {
            CalculationMethod.MuslimWorldLeague => new(18, 17, 0, 0),
            CalculationMethod.ISNA => new(15, 15, 0, 0),
            CalculationMethod.Egyptian => new(19.5, 17.5, 0, 0),
            CalculationMethod.Makkah => new(18.5, 0, 90, 0), // Isha 90 min after Maghrib
            CalculationMethod.Karachi => new(18, 18, 0, 0),
            CalculationMethod.Tehran => new(17.7, 14, 0, 0),
            CalculationMethod.Jafari => new(16, 14, 0, 0),
            CalculationMethod.Gulf => new(19.5, 0, 90, 0), // Isha 90 min after Maghrib
            CalculationMethod.Kuwait => new(18, 17.5, 0, 0),
            CalculationMethod.Qatar => new(18, 0, 90, 0), // Isha 90 min after Maghrib
            CalculationMethod.Singapore => new(20, 18, 0, 0),
            CalculationMethod.Turkey => new(18, 17, 0, 0),
            CalculationMethod.MoonsightingCommittee => new(18, 18, 0, 0),
            CalculationMethod.Dubai => new(18.2, 18.2, 0, 0),
            _ => new(18, 17, 0, 0)
        };
    }

    private static TimeSpan AdjustIshaForHighLatitudes(TimeSpan isha, TimeSpan maghrib, double lat)
    {
        // For high latitudes where Isha might not be calculable
        if (isha == TimeSpan.Zero || isha < maghrib)
        {
            // Use 1.5 hours after Maghrib as fallback
            var hours = maghrib.Hours + 1;
            var minutes = maghrib.Minutes + 30;
            if (minutes >= 60)
            {
                hours += 1;
                minutes -= 60;
            }
            return new TimeSpan(hours, minutes, 0);
        }

        return isha;
    }

    private record CalculationParameters(double FajrAngle, double IshaAngle, int IshaMinutesAfterMaghrib, int MaghribMinutesAfterSunset);

    private class PrayerTimeTimes
    {
        public TimeSpan Fajr { get; set; }
        public TimeSpan Sunrise { get; set; }
        public TimeSpan Dhuhr { get; set; }
        public TimeSpan Asr { get; set; }
        public TimeSpan Maghrib { get; set; }
        public TimeSpan Isha { get; set; }
    }

    #endregion
}

/// <summary>
/// Extension methods for PrayerTimes.
/// </summary>
public static class PrayerTimesExtensions
{
    /// <summary>
    /// Converts a DateOnly and TimeSpan to DateTime.
    /// </summary>
    public static DateTime ToDateTime(this DateOnly date, TimeSpan time)
    {
        // Handle time spans that might exceed 24 hours (overflow)
        var totalHours = (int)time.TotalHours;
        var days = totalHours / 24;
        var hours = totalHours % 24;
        var minutes = time.Minutes;
        var seconds = time.Seconds;

        return new DateTime(date.Year, date.Month, date.Day, hours, minutes, seconds).AddDays(days);
    }

    /// <summary>
    /// Formats a TimeSpan to a display string.
    /// </summary>
    public static string FormatDisplay(this TimeSpan time, bool use24Hour = true)
    {
        if (use24Hour)
        {
            return time.ToString(@"hh\:mm");
        }

        var hours = time.Hours;
        var minutes = time.Minutes;
        var ampm = hours >= 12 ? "PM" : "AM";
        var displayHours = hours % 12;
        if (displayHours == 0) displayHours = 12;

        return $"{displayHours}:{minutes:D2} {ampm}";
    }
}
