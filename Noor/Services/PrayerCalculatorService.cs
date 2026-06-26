using Noor.Models;

namespace Noor.Services;

/// <summary>
/// Service for calculating Islamic prayer times using astronomical calculations.
/// </summary>
public class PrayerCalculatorService
{
    private const double EarthRadius = 6378.137; // km

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
        var jd = CalculateJulianDay(date.Year, date.Month, date.Day);
        var times = CalculateTimes(jd, lat, lng, tz, calculationMethod);

        // Apply Asr Madhab adjustment
        times.Asr = CalculateAsr(jd, lat, lng, tz, madhab);

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

    private static double CalculateJulianDay(int year, int month, int day)
    {
        if (month <= 2)
        {
            year -= 1;
            month += 12;
        }

        var A = Math.Floor(year / 100.0);
        var B = 2 - A + Math.Floor(A / 4.0);
        return Math.Floor(365.25 * (year + 4716)) + Math.Floor(30.6001 * (month + 1)) + day + B - 1524.5;
    }

    private static double CalculateSunPosition(double jd)
    {
        var D = jd - 2451545.0;
        var g = 357.529 + 0.98560028 * D;
        var q = 280.459 + 0.98564736 * D;
        var L = q + 1.915 * Math.Sin(g * Math.PI / 180) + 0.020 * Math.Sin(2 * g * Math.PI / 180);

        var e = 23.439 - 0.00000036 * D;
        var RA = Math.Atan2(Math.Cos(e * Math.PI / 180) * Math.Sin(L * Math.PI / 180), Math.Cos(L * Math.PI / 180)) * 180 / Math.PI;

        return (RA + 360) % 360;
    }

    private static (double declination, double equationOfTime) CalculateSunParameters(double jd)
    {
        var D = jd - 2451545.0;
        var g = 357.529 + 0.98560028 * D;
        var q = 280.459 + 0.98564736 * D;
        var L = q + 1.915 * Math.Sin(g * Math.PI / 180) + 0.020 * Math.Sin(2 * g * Math.PI / 180);

        var e = 23.439 - 0.00000036 * D;
        var RA = Math.Atan2(Math.Cos(e * Math.PI / 180) * Math.Sin(L * Math.PI / 180), Math.Cos(L * Math.PI / 180)) * 180 / Math.PI;

        var declination = Math.Asin(Math.Sin(e * Math.PI / 180) * Math.Sin(L * Math.PI / 180)) * 180 / Math.PI;

        // Fix RA to be in 0-24 range first, then calculate eqT
        var RA_hours = RA / 15.0;
        RA_hours = RA_hours - Math.Floor(RA_hours);
        if (RA_hours < 0) RA_hours += 24;

        var q_hours = q / 15.0;
        q_hours = q_hours - Math.Floor(q_hours);
        if (q_hours < 0) q_hours += 24;

        var EqT = q_hours - RA_hours;

        return (declination, EqT);
    }

    private static TimeSpan FixHour(double hour)
    {
        hour = hour - Math.Floor(hour);
        if (hour < 0) hour += 24;
        return TimeSpan.Zero;
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

    private static PrayerTimeTimes CalculateTimes(double jd, double lat, double lng, double tz, CalculationMethod method)
    {
        var (declination, eqT) = CalculateSunParameters(jd);
        var calcParams = GetCalculationParameters(method);

        var D = jd - 2451545.0;

        // Calculate times using timezone
        var times = new PrayerTimeTimes();

        times.Maghrib = CalculateTime(-calcParams.Maghrib, lat, lng, tz, declination, eqT, calcParams);
        times.Isha = CalculateTime(calcParams.Isha, lat, lng, tz, declination, eqT, calcParams);

        // Dhuhr formula: 12 + timezone - longitude/15 - eqT
        var dhuhrHour = NormalizeHour(12 + tz - lng / 15.0 - eqT);
        times.Dhuhr = ConvertHourToTimeSpan(dhuhrHour);

        times.Sunrise = CalculateTime(-0.833, lat, lng, tz, declination, eqT, calcParams);
        times.Fajr = CalculateTime(calcParams.Fajr, lat, lng, tz, declination, eqT, calcParams);

        // Asr is calculated separately based on Madhab
        return times;
    }

    private static TimeSpan CalculateAsr(double jd, double lat, double lng, double tz, Madhab madhab)
    {
        var (declination, eqT) = CalculateSunParameters(jd);
        var D = jd - 2451545.0;

        // Shafi: shadow = 1 + length, Hanafi: shadow = 2 * length
        var shadowFactor = (madhab == Madhab.Hanafi) ? 2.0 : 1.0;

        var altDiff = Math.Abs(lat - declination) * Math.PI / 180.0;
        var arc = -Math.Atan(1.0 / (shadowFactor + Math.Tan(altDiff))) * 180 / Math.PI;

        return CalculateTime(arc, lat, lng, tz, declination, eqT, new CalculationParameters(0, 0, 0));
    }

    private static TimeSpan CalculateTime(double angle, double lat, double lng, double tz, double declination, double eqT, CalculationParameters parameters)
    {
        try
        {
            var latRad = lat * Math.PI / 180.0;
            var declRad = declination * Math.PI / 180.0;
            var angleRad = angle * Math.PI / 180.0;

            var num = -Math.Sin(angleRad) - Math.Sin(latRad) * Math.Sin(declRad);
            var den = Math.Cos(latRad) * Math.Cos(declRad);

            if (Math.Abs(den) < 0.0001)
            {
                return TimeSpan.Zero;
            }

            var value = num / den;

            if (value < -1) value = -1;
            if (value > 1) value = 1;

            var h = Math.Acos(value) * 180 / Math.PI / 15.0;

            // Prayer time formula: T = 12 + timezone - longitude/15 - eqT - H
            var time = 12 + tz - lng / 15.0 - eqT - (angle > 0 ? h : -h);

            // Normalize the time to be between 0 and 24
            time = NormalizeHour(time);

            var hours = (int)time;
            var minutes = (int)((time - hours) * 60);

            return new TimeSpan(hours, minutes, 0);
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }

    private static CalculationParameters GetCalculationParameters(CalculationMethod method)
    {
        return method switch
        {
            CalculationMethod.MuslimWorldLeague => new CalculationParameters(18, 17, 90),
            CalculationMethod.ISNA => new CalculationParameters(15, 15, 90),
            CalculationMethod.Egyptian => new CalculationParameters(19.5, 17.5, 90),
            CalculationMethod.Makkah => new CalculationParameters(18.5, 90, 90),
            CalculationMethod.Karachi => new CalculationParameters(18, 18, 90),
            CalculationMethod.Tehran => new CalculationParameters(17.7, 14, 90),
            CalculationMethod.Jafari => new CalculationParameters(16, 14, 90),
            CalculationMethod.Gulf => new CalculationParameters(19.5, 90, 90),
            CalculationMethod.Kuwait => new CalculationParameters(18, 17.5, 90),
            CalculationMethod.Qatar => new CalculationParameters(18, 18, 90),
            CalculationMethod.Singapore => new CalculationParameters(20, 18, 90),
            CalculationMethod.Turkey => new CalculationParameters(18, 17, 90),
            CalculationMethod.MoonsightingCommittee => new CalculationParameters(18, 18, 90),
            CalculationMethod.Dubai => new CalculationParameters(18.2, 18.2, 90),
            _ => new CalculationParameters(18, 17, 90)
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

    private record CalculationParameters(double Fajr, double Maghrib, double Isha);

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
            return time.ToString(@"HH\:mm");
        }

        var hours = time.Hours;
        var minutes = time.Minutes;
        var ampm = hours >= 12 ? "PM" : "AM";
        var displayHours = hours % 12;
        if (displayHours == 0) displayHours = 12;

        return $"{displayHours}:{minutes:D2} {ampm}";
    }
}
