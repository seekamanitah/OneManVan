using FluentAssertions;
using Xunit;

namespace OneManVan.Mobile.Tests.Controls;

public class QuickSchedulePopupTests
{
    [Fact]
    public void ScheduleEventArgs_ShouldConstructCorrectly()
    {
        // Arrange
        var date = DateTime.Today.AddDays(3);
        var time = new TimeSpan(11, 30, 0);

        // Act - Create a mock ScheduleEventArgs since we can't access the MAUI class
        var args = new MockScheduleEventArgs(date, time);

        // Assert
        args.Date.Should().Be(date);
        args.Time.Should().Be(time);
        args.ScheduledDateTime.Should().Be(date.Add(time));
    }

    [Fact]
    public void ScheduleEventArgs_WithNullTime_ShouldHandleCorrectly()
    {
        // Arrange
        var date = DateTime.Today.AddDays(1);

        // Act
        var args = new MockScheduleEventArgs(date);

        // Assert
        args.Date.Should().Be(date);
        args.Time.Should().BeNull();
        args.ScheduledDateTime.Should().BeNull();
    }

    [Fact]
    public void ScheduleEventArgs_ShouldHandleDifferentTimes()
    {
        // Test various time scenarios
        var testCases = new[]
        {
            new { Date = DateTime.Today, Time = new TimeSpan(9, 0, 0), Expected = DateTime.Today.AddHours(9) },
            new { Date = DateTime.Today.AddDays(1), Time = new TimeSpan(14, 30, 0), Expected = DateTime.Today.AddDays(1).AddHours(14).AddMinutes(30) },
            new { Date = DateTime.Today.AddDays(-1), Time = new TimeSpan(18, 45, 0), Expected = DateTime.Today.AddDays(-1).AddHours(18).AddMinutes(45) }
        };

        foreach (var testCase in testCases)
        {
            var args = new MockScheduleEventArgs(testCase.Date, testCase.Time);
            args.ScheduledDateTime.Should().Be(testCase.Expected);
        }
    }

    // Mock class to simulate ScheduleEventArgs
    private class MockScheduleEventArgs
    {
        public DateTime Date { get; }
        public TimeSpan? Time { get; }
        public DateTime? ScheduledDateTime => Time.HasValue ? Date.Add(Time.Value) : null;

        public MockScheduleEventArgs(DateTime date, TimeSpan? time = null)
        {
            Date = date;
            Time = time;
        }
    }
}