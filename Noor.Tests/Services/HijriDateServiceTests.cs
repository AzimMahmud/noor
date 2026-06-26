using FluentAssertions;
using Noor.Services;
using Xunit;

namespace Noor.Tests.Services;

public class HijriDateServiceTests
{
    [Fact]
    public void GetHijriYear_Month_Day_AreInternallyConsistent()
    {
        var date = new DateTime(2024, 6, 21);

        var year = HijriDateService.GetHijriYear(date);
        var month = HijriDateService.GetHijriMonth(date);
        var day = HijriDateService.GetHijriDay(date);

        year.Should().BeGreaterThan(1400);
        month.Should().BeInRange(1, 12);
        day.Should().BeInRange(1, 30);

        var formatted = HijriDateService.ToHijriString(date);
        formatted.Should().Contain(day.ToString());
        formatted.Should().Contain(year.ToString());
        formatted.Should().Contain("AH");
    }

    [Theory]
    [InlineData(1, "Muharram", "محرم")]
    [InlineData(2, "Safar", "صفر")]
    [InlineData(9, "Ramadan", "رمضان")]
    [InlineData(10, "Shawwal", "شوال")]
    [InlineData(12, "Dhu al-Hijjah", "ذو الحجة")]
    public void HijriMonthNames_AreCorrect(int month, string english, string arabic)
    {
        HijriDateService.GetHijriMonthNameEnglish(month).Should().Be(english);
        HijriDateService.GetHijriMonthNameArabic(month).Should().Be(arabic);
    }

    [Fact]
    public void AllTwelveMonths_HaveNames()
    {
        for (int m = 1; m <= 12; m++)
        {
            HijriDateService.GetHijriMonthNameEnglish(m).Should().NotBeNullOrEmpty();
            HijriDateService.GetHijriMonthNameArabic(m).Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void HijriToGregorian_RoundTrips()
    {
        var date = new DateTime(2024, 6, 21);
        var year = HijriDateService.GetHijriYear(date);
        var month = HijriDateService.GetHijriMonth(date);
        var day = HijriDateService.GetHijriDay(date);

        var gregorian = HijriDateService.HijriToGregorian(year, month, day);

        HijriDateService.GetHijriYear(gregorian).Should().Be(year);
        HijriDateService.GetHijriMonth(gregorian).Should().Be(month);
        HijriDateService.GetHijriDay(gregorian).Should().Be(day);
    }

    [Fact]
    public void IsRamadan_MatchesMonthNine()
    {
        // Find a date that falls in Ramadan by scanning forward from a known point.
        DateTime ramadanDate = new(2024, 1, 1);
        while (HijriDateService.GetHijriMonth(ramadanDate) != 9)
            ramadanDate = ramadanDate.AddDays(1);

        HijriDateService.IsRamadan(ramadanDate).Should().BeTrue();

        DateTime nonRamadanDate = ramadanDate.AddMonths(1);
        HijriDateService.IsRamadan(nonRamadanDate).Should().BeFalse();
    }

    [Theory]
    [InlineData(1, 1, "Islamic New Year")]
    [InlineData(1, 10, "Day of Ashura")]
    [InlineData(3, 12, "Mawlid al-Nabi")]
    [InlineData(9, 1, "Start of Ramadan")]
    [InlineData(10, 1, "Eid al-Fitr")]
    [InlineData(12, 9, "Day of Arafah")]
    [InlineData(12, 10, "Eid al-Adha")]
    public void IslamicEvents_ArePresent(int month, int day, string eventName)
    {
        var events = HijriDateService.GetIslamicEvents(month);
        events.Should().ContainKey(day);
        events[day].Should().Be(eventName);
    }

    [Fact]
    public void GetDaysInHijriMonth_Is29Or30()
    {
        var date = new DateTime(2024, 6, 21);
        var year = HijriDateService.GetHijriYear(date);

        for (int m = 1; m <= 12; m++)
        {
            var days = HijriDateService.GetDaysInHijriMonth(year, m);
            days.Should().BeGreaterThanOrEqualTo(29);
            days.Should().BeLessThanOrEqualTo(30);
        }
    }

    [Fact]
    public void GetFirstDayOfWeek_IsInRange0To6()
    {
        var date = new DateTime(2024, 6, 21);
        var year = HijriDateService.GetHijriYear(date);
        var month = HijriDateService.GetHijriMonth(date);

        HijriDateService.GetFirstDayOfWeek(year, month).Should().BeInRange(0, 6);
    }
}
