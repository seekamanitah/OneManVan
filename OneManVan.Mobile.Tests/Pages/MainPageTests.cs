using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using Xunit;

namespace OneManVan.Mobile.Tests.Pages;

public class MainPageTests : TestBase
{
    [Fact]
    public async Task DashboardData_ShouldLoadCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - Simulate loading dashboard data
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var todayJobs = await DbContext.Jobs
            .Where(j => j.ScheduledDate.HasValue &&
                       j.ScheduledDate.Value.Date == today &&
                       j.Status != JobStatus.Cancelled)
            .ToListAsync();

        var weekJobs = await DbContext.Jobs
            .Where(j => j.ScheduledDate.HasValue &&
                       j.ScheduledDate.Value.Date >= today &&
                       j.ScheduledDate.Value.Date <= today.AddDays(6) &&
                       j.Status != JobStatus.Cancelled)
            .ToListAsync();

        var overdueJobs = await DbContext.Jobs
            .Where(j => j.ScheduledDate.HasValue &&
                       j.ScheduledDate.Value.Date < today &&
                       j.Status != JobStatus.Completed &&
                       j.Status != JobStatus.Closed &&
                       j.Status != JobStatus.Cancelled)
            .ToListAsync();

        // Assert
        todayJobs.Should().HaveCount(0); // No jobs scheduled for today in test data
        weekJobs.Should().HaveCount(2); // 2 jobs scheduled in the coming week
        overdueJobs.Should().HaveCount(0); // No overdue jobs in test data
    }

    [Fact]
    public async Task MonthCalendarData_ShouldLoadCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();
        var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var nextMonth = currentMonth.AddMonths(1);

        // Act
        var monthJobs = await DbContext.Jobs
            .Where(j => j.ScheduledDate.HasValue &&
                       j.ScheduledDate.Value.Date >= currentMonth &&
                       j.ScheduledDate.Value.Date < nextMonth &&
                       j.Status != JobStatus.Cancelled)
            .ToListAsync();

        var jobCountsByDate = monthJobs
            .GroupBy(j => j.ScheduledDate!.Value.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        // Assert
        monthJobs.Should().HaveCount(2);
        jobCountsByDate.Should().ContainKey(DateTime.Today.AddDays(1));
        jobCountsByDate[DateTime.Today.AddDays(1)].Should().Be(1);
        jobCountsByDate.Should().ContainKey(DateTime.Today.AddDays(2));
        jobCountsByDate[DateTime.Today.AddDays(2)].Should().Be(1);
    }

    [Fact]
    public async Task JobStatusTransitions_ShouldWorkCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();
        var job = await DbContext.Jobs.FirstAsync();

        // Act - Simulate job status transitions
        // Simulate starting the job
        var savedJob = await DbContext.Jobs.FindAsync(job.Id);
        savedJob!.Status = JobStatus.InProgress;
        savedJob.StartedAt = DateTime.Now;
        await DbContext.SaveChangesAsync();

        // Simulate completing the job
        savedJob.Status = JobStatus.Completed;
        savedJob.CompletedAt = DateTime.Now;
        // Note: ActualHours is computed from TimeEntries, can't be set directly
        await DbContext.SaveChangesAsync();

        // Assert
        var updatedJob = await DbContext.Jobs.FindAsync(job.Id);
        updatedJob.Should().NotBeNull();
        updatedJob!.Status.Should().Be(JobStatus.Completed);
        updatedJob.StartedAt.Should().NotBeNull();
        updatedJob.CompletedAt.Should().NotBeNull();
    }
}