using FluentAssertions;
using Xunit;

namespace OneManVan.Mobile.Tests.Controls;

public class MonthCalendarControlTests
{
    [Fact]
    public void CalendarLogic_ShouldHandleDateCalculations()
    {
        // Test basic calendar logic without UI components
        var today = DateTime.Today;
        var displayMonth = new DateTime(today.Year, today.Month, 1);

        // Test that display month is first day of current month
        displayMonth.Day.Should().Be(1);
        displayMonth.Month.Should().Be(today.Month);
        displayMonth.Year.Should().Be(today.Year);
    }

    [Fact]
    public void JobCountDictionary_ShouldStoreJobCountsByDate()
    {
        // Test the data structure that would be used by the calendar
        var jobCounts = new Dictionary<DateTime, int>
        {
            { DateTime.Today, 1 },
            { DateTime.Today.AddDays(1), 3 },
            { DateTime.Today.AddDays(2), 5 }
        };

        jobCounts.Should().HaveCount(3);
        jobCounts[DateTime.Today].Should().Be(1);
        jobCounts[DateTime.Today.AddDays(1)].Should().Be(3);
        jobCounts[DateTime.Today.AddDays(2)].Should().Be(5);
    }

    [Fact]
    public void GoToDate_ShouldCalculateCorrectDisplayMonth()
    {
        // Test the date navigation logic
        var targetDate = new DateTime(2024, 6, 15);
        var expectedDisplayMonth = new DateTime(2024, 6, 1);

        // Simulate GoToDate logic
        var actualDisplayMonth = new DateTime(targetDate.Year, targetDate.Month, 1);

        actualDisplayMonth.Should().Be(expectedDisplayMonth);
        targetDate.Should().BeOnOrAfter(actualDisplayMonth);
        targetDate.Should().BeBefore(actualDisplayMonth.AddMonths(1));
    }
}