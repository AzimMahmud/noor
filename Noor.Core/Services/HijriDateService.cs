using System.Globalization;

namespace Noor.Services;

/// <summary>
/// Service for converting Gregorian dates to Hijri (Islamic calendar).
/// </summary>
public static class HijriDateService
{
    private static readonly UmAlQuraCalendar _hijriCalendar = new();

    /// <summary>
    /// Converts a DateTime to its Hijri (Islamic calendar) representation.
    /// </summary>
    public static string ToHijriString(DateTime date)
    {
        var year = _hijriCalendar.GetYear(date);
        var month = _hijriCalendar.GetMonth(date);
        var day = _hijriCalendar.GetDayOfMonth(date);

        var monthName = GetHijriMonthNameArabic(month);
        var monthNameEnglish = GetHijriMonthNameEnglish(month);

        return $"{day} {monthName} {year} AH • {monthNameEnglish} {day}, {year}";
    }

    /// <summary>
    /// Gets the Hijri month name in Arabic.
    /// </summary>
    public static string GetHijriMonthNameArabic(int month) => month switch
    {
        1 => "محرم",
        2 => "صفر",
        3 => "ربيع الأول",
        4 => "ربيع الآخر",
        5 => "جمادى الأولى",
        6 => "جمادى الآخرة",
        7 => "رجب",
        8 => "شعبان",
        9 => "رمضان",
        10 => "شوال",
        11 => "ذو القعدة",
        12 => "ذو الحجة",
        _ => ""
    };

    /// <summary>
    /// Gets the Hijri month name in English.
    /// </summary>
    public static string GetHijriMonthNameEnglish(int month) => month switch
    {
        1 => "Muharram",
        2 => "Safar",
        3 => "Rabi' al-Awwal",
        4 => "Rabi' al-Thani",
        5 => "Jumada al-Awwal",
        6 => "Jumada al-Thani",
        7 => "Rajab",
        8 => "Sha'ban",
        9 => "Ramadan",
        10 => "Shawwal",
        11 => "Dhu al-Qi'dah",
        12 => "Dhu al-Hijjah",
        _ => ""
    };

    /// <summary>
    /// Gets the Arabic day name for display.
    /// </summary>
    public static string GetDayNameArabic(int dayOfWeek) => dayOfWeek switch
    {
        0 => "الأحد",
        1 => "الإثنين",
        2 => "الثلاثاء",
        3 => "الأربعاء",
        4 => "الخميس",
        5 => "الجمعة",
        6 => "السبت",
        _ => ""
    };

    /// <summary>
    /// Checks if a given Gregorian date is in the month of Ramadan.
    /// </summary>
    public static bool IsRamadan(DateTime date)
    {
        return _hijriCalendar.GetMonth(date) == 9;
    }

    /// <summary>
    /// Gets the Hijri year for a given Gregorian date.
    /// </summary>
    public static int GetHijriYear(DateTime date)
    {
        return _hijriCalendar.GetYear(date);
    }

    /// <summary>
    /// Gets the Hijri month number (1-12) for a given Gregorian date.
    /// </summary>
    public static int GetHijriMonth(DateTime date)
    {
        return _hijriCalendar.GetMonth(date);
    }

    /// <summary>
    /// Gets the Hijri day of month for a given Gregorian date.
    /// </summary>
    public static int GetHijriDay(DateTime date)
    {
        return _hijriCalendar.GetDayOfMonth(date);
    }

    /// <summary>
    /// Gets the number of days in a given Hijri month.
    /// </summary>
    public static int GetDaysInHijriMonth(int hijriYear, int hijriMonth)
    {
        return _hijriCalendar.GetDaysInMonth(hijriYear, hijriMonth);
    }

    /// <summary>
    /// Converts a Hijri date (year, month, day) to its Gregorian DateTime.
    /// </summary>
    public static DateTime HijriToGregorian(int hijriYear, int hijriMonth, int hijriDay)
    {
        return _hijriCalendar.ToDateTime(hijriYear, hijriMonth, hijriDay, 0, 0, 0, 0);
    }

    /// <summary>
    /// Gets the Gregorian DateTime for the first day of a Hijri month.
    /// </summary>
    public static DateTime GetFirstDayOfHijriMonth(int hijriYear, int hijriMonth)
    {
        return HijriToGregorian(hijriYear, hijriMonth, 1);
    }

    /// <summary>
    /// Gets the day of week (0=Sunday) for the first day of a Hijri month.
    /// </summary>
    public static int GetFirstDayOfWeek(int hijriYear, int hijriMonth)
    {
        var firstDay = GetFirstDayOfHijriMonth(hijriYear, hijriMonth);
        return (int)firstDay.DayOfWeek;
    }

    /// <summary>
    /// Gets notable Islamic dates for a given Hijri month.
    /// Returns a dictionary of day -> event name.
    /// </summary>
    public static Dictionary<int, string> GetIslamicEvents(int hijriMonth) => hijriMonth switch
    {
        1 => new Dictionary<int, string>
        {
            [1] = "Islamic New Year",
            [10] = "Day of Ashura"
        },
        3 => new Dictionary<int, string>
        {
            [12] = "Mawlid al-Nabi"
        },
        7 => new Dictionary<int, string>
        {
            [27] = "Isra & Mi'raj"
        },
        8 => new Dictionary<int, string>
        {
            [15] = "Shab-e-Barat"
        },
        9 => new Dictionary<int, string>
        {
            [1] = "Start of Ramadan"
        },
        10 => new Dictionary<int, string>
        {
            [1] = "Eid al-Fitr"
        },
        12 => new Dictionary<int, string>
        {
            [9] = "Day of Arafah",
            [10] = "Eid al-Adha"
        },
        _ => new Dictionary<int, string>()
    };
}
