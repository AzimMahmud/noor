using FluentAssertions;
using Noor.Models;
using Noor.Services;
using Xunit;

namespace Noor.Tests.Services;

public class PrayerCalculatorServiceTests
{
    private static readonly Coordinates Makkah = new(21.4225, 39.8262, "Makkah", "Saudi Arabia", 3.0);

    private readonly PrayerCalculatorService _calculator = new();

    [Fact]
    public void CalculatePrayerTimes_ReturnsAllFivePrayers_AndSunrise()
    {
        var times = _calculator.CalculatePrayerTimes(DateOnly.FromDateTime(DateTime.Today), Makkah);

        times.Should().NotBeNull();
        times.Fajr.Should().BeGreaterThan(TimeSpan.Zero);
        times.Sunrise.Should().BeGreaterThan(TimeSpan.Zero);
        times.Dhuhr.Should().BeGreaterThan(TimeSpan.Zero);
        times.Asr.Should().BeGreaterThan(TimeSpan.Zero);
        times.Maghrib.Should().BeGreaterThan(TimeSpan.Zero);
        times.Isha.Should().BeGreaterThan(TimeSpan.Zero);

        times.DailyPrayers.Should().HaveCount(5);
        times.DailyPrayers.Keys.Should().BeEquivalentTo(new[]
        {
            PrayerType.Fajr, PrayerType.Dhuhr, PrayerType.Asr, PrayerType.Maghrib, PrayerType.Isha
        });
    }

    [Theory]
    [InlineData(CalculationMethod.MuslimWorldLeague)]
    [InlineData(CalculationMethod.ISNA)]
    [InlineData(CalculationMethod.Egyptian)]
    [InlineData(CalculationMethod.Makkah)]
    [InlineData(CalculationMethod.Karachi)]
    [InlineData(CalculationMethod.Tehran)]
    [InlineData(CalculationMethod.Jafari)]
    [InlineData(CalculationMethod.Gulf)]
    [InlineData(CalculationMethod.Kuwait)]
    [InlineData(CalculationMethod.Qatar)]
    [InlineData(CalculationMethod.Singapore)]
    [InlineData(CalculationMethod.Turkey)]
    [InlineData(CalculationMethod.MoonsightingCommittee)]
    [InlineData(CalculationMethod.Dubai)]
    public void EveryCalculationMethod_ProducesOrderedValidTimes(CalculationMethod method)
    {
        var times = _calculator.CalculatePrayerTimes(
            DateOnly.FromDateTime(new DateTime(2024, 6, 21)), // summer solstice
            Makkah, Madhab.Hanafi, method);

        times.Fajr.Should().BeGreaterThan(TimeSpan.Zero, "Fajr must be a positive time");
        times.Isha.Should().BeGreaterThan(TimeSpan.Zero, "Isha must be a positive time");
        times.Isha.Should().BeGreaterThan(times.Maghrib, "Isha must come after Maghrib");
        times.Sunrise.Should().BeGreaterThan(times.Fajr, "Sunrise must come after Fajr");
        times.Maghrib.Should().BeGreaterThan(times.Asr, "Maghrib must come after Asr");

        // All times within a single day.
        times.Fajr.Should().BeLessThan(TimeSpan.FromHours(24));
        times.Isha.Should().BeLessThan(TimeSpan.FromHours(24));
    }

    [Fact]
    public void Times_AreChronologicallyOrdered_ForMakkah()
    {
        var times = _calculator.CalculatePrayerTimes(DateOnly.FromDateTime(DateTime.Today), Makkah);

        times.Fajr.Should().BeLessThan(times.Sunrise);
        times.Sunrise.Should().BeLessThan(times.Dhuhr);
        times.Dhuhr.Should().BeLessThan(times.Asr);
        times.Asr.Should().BeLessThan(times.Maghrib);
        times.Maghrib.Should().BeLessThan(times.Isha);
    }

    [Fact]
    public void Dhuhr_IsNearLocalMidday_ForMakkah()
    {
        var times = _calculator.CalculatePrayerTimes(DateOnly.FromDateTime(DateTime.Today), Makkah);

        // Makkah is UTC+3; solar noon should land roughly between 11:30 and 12:30 local.
        times.Dhuhr.TotalHours.Should().BeGreaterThan(11.0);
        times.Dhuhr.TotalHours.Should().BeLessThan(13.0);
    }

    /// <summary>
    /// Regression guard: known-good ranges for Makkah on the 2024 summer solstice
    /// (Muslim World League, Hanafi). Catches the previously-broken morning/evening
    /// direction logic that produced a ~19:00 "sunrise".
    /// </summary>
    [Fact]
    public void Makkah_SummerSolstice_TimesAreWithinPublishedRanges()
    {
        var times = _calculator.CalculatePrayerTimes(
            new DateOnly(2024, 6, 21), Makkah, Madhab.Hanafi, CalculationMethod.MuslimWorldLeague);

        times.Fajr.TotalHours.Should().BeInRange(4.0, 5.0, "Fajr in Makkah summer is ~04:20");
        times.Sunrise.TotalHours.Should().BeInRange(5.0, 6.0, "Sunrise in Makkah summer is ~05:35");
        times.Dhuhr.TotalHours.Should().BeInRange(12.0, 12.5, "Dhuhr in Makkah is ~12:18");
        times.Asr.TotalHours.Should().BeInRange(15.5, 17.5, "Hanafi Asr in Makkah summer is ~16:56");
        times.Maghrib.TotalHours.Should().BeInRange(18.5, 19.5, "Maghrib in Makkah summer is ~19:00");
        times.Isha.TotalHours.Should().BeInRange(19.5, 21.0, "Isha (MWL) in Makkah summer is ~20:20");
    }

    [Fact]
    public void Hanafi_Asr_IsLaterThan_Shafi_Asr()
    {
        var date = DateOnly.FromDateTime(DateTime.Today);

        var shafi = _calculator.CalculatePrayerTimes(date, Makkah, Madhab.Shafi);
        var hanafi = _calculator.CalculatePrayerTimes(date, Makkah, Madhab.Hanafi);

        hanafi.Asr.Should().BeGreaterThanOrEqualTo(shafi.Asr,
            "the Hanafi school computes Asr from a longer shadow length");
    }

    [Fact]
    public void CalculateForDateRange_ReturnsRequestedNumberOfDays()
    {
        var results = _calculator.CalculateForDateRange(
            DateOnly.FromDateTime(DateTime.Today), 7, Makkah);

        results.Should().HaveCount(7);
        results.Should().OnlyContain(x => x != null);
    }

    [Fact]
    public void GetPrayerTime_ReturnsExpectedTime_ForEachPrayer()
    {
        var times = _calculator.CalculatePrayerTimes(DateOnly.FromDateTime(DateTime.Today), Makkah);

        times.GetPrayerTime(PrayerType.Fajr).Should().Be(times.Fajr);
        times.GetPrayerTime(PrayerType.Dhuhr).Should().Be(times.Dhuhr);
        times.GetPrayerTime(PrayerType.Asr).Should().Be(times.Asr);
        times.GetPrayerTime(PrayerType.Maghrib).Should().Be(times.Maghrib);
        times.GetPrayerTime(PrayerType.Isha).Should().Be(times.Isha);
    }

    [Theory]
    [InlineData(PrayerType.Fajr, "Fajr", "الفجر")]
    [InlineData(PrayerType.Dhuhr, "Dhuhr", "الظهر")]
    [InlineData(PrayerType.Asr, "Asr", "العصر")]
    [InlineData(PrayerType.Maghrib, "Maghrib", "المغرب")]
    [InlineData(PrayerType.Isha, "Isha", "العشاء")]
    public void PrayerType_HasCorrectDisplayAndArabicNames(PrayerType prayer, string english, string arabic)
    {
        prayer.GetDisplayName().Should().Be(english);
        prayer.GetArabicName().Should().Be(arabic);
    }

    [Theory]
    [InlineData(6, 30, 0, true, "06:30")]
    [InlineData(6, 30, 0, false, "6:30 AM")]
    [InlineData(18, 5, 0, true, "18:05")]
    [InlineData(0, 0, 0, false, "12:00 AM")]
    [InlineData(12, 0, 0, false, "12:00 PM")]
    public void FormatDisplay_FormatsCorrectly(int h, int m, int s, bool use24h, string expected)
    {
        var formatted = new TimeSpan(h, m, s).FormatDisplay(use24h);
        formatted.Should().Be(expected);
    }
}
