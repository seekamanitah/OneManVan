using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using Xunit;

namespace OneManVan.Mobile.Tests.Integration;

public class DatabaseIntegrationTests : TestBase
{
    [Fact]
    public async Task CreateAndRetrieveCustomer_ShouldWorkCorrectly()
    {
        // Arrange
        var customer = new Customer
        {
            Name = "Integration Test Customer",
            Email = "integration@test.com",
            Phone = "(555) 123-4567"
        };

        // Act
        await DbContext.Customers.AddAsync(customer);
        await DbContext.SaveChangesAsync();

        var retrievedCustomer = await DbContext.Customers.FindAsync(customer.Id);

        // Assert
        retrievedCustomer.Should().NotBeNull();
        retrievedCustomer!.Name.Should().Be("Integration Test Customer");
        retrievedCustomer.Email.Should().Be("integration@test.com");
    }

    [Fact]
    public async Task CreateJobWithCustomerAndAsset_ShouldWorkCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();

        var job = new Job
        {
            Title = "Integration Test Job",
            Description = "Testing job creation with relationships",
            CustomerId = 1, // From seeded data
            ScheduledDate = DateTime.Today.AddDays(7),
            ArrivalWindowStart = new TimeSpan(10, 0, 0),
            ArrivalWindowEnd = new TimeSpan(12, 0, 0),
            Priority = JobPriority.Normal,
            Status = JobStatus.Scheduled,
            EstimatedHours = 2.5m,
            Total = 200.00m
        };

        // Act
        await DbContext.Jobs.AddAsync(job);
        await DbContext.SaveChangesAsync();

        var retrievedJob = await DbContext.Jobs
            .Include(j => j.Customer)
            .FirstOrDefaultAsync(j => j.Title == "Integration Test Job");

        // Assert
        retrievedJob.Should().NotBeNull();
        retrievedJob!.Customer.Should().NotBeNull();
        retrievedJob.Customer.Name.Should().Be("John Smith");
        retrievedJob.ScheduledDate.Should().Be(DateTime.Today.AddDays(7));
    }

    [Fact]
    public async Task CalculateJobStatistics_ShouldReturnCorrectCounts()
    {
        // Arrange
        await SeedTestDataAsync();

        // Add more test jobs with different statuses
        var additionalJobs = new[]
        {
            new Job
            {
                Title = "Completed Job",
                CustomerId = 1,
                ScheduledDate = DateTime.Today.AddDays(-1),
                Status = JobStatus.Completed,
                Total = 150.00m
            },
            new Job
            {
                Title = "In Progress Job",
                CustomerId = 2,
                ScheduledDate = DateTime.Today,
                Status = JobStatus.InProgress,
                Total = 100.00m
            },
            new Job
            {
                Title = "Cancelled Job",
                CustomerId = 1,
                ScheduledDate = DateTime.Today.AddDays(3),
                Status = JobStatus.Cancelled,
                Total = 75.00m
            }
        };

        await DbContext.Jobs.AddRangeAsync(additionalJobs);
        await DbContext.SaveChangesAsync();

        // Act
        var totalJobs = await DbContext.Jobs.CountAsync();
        var scheduledJobs = await DbContext.Jobs.CountAsync(j => j.Status == JobStatus.Scheduled);
        var completedJobs = await DbContext.Jobs.CountAsync(j => j.Status == JobStatus.Completed);
        var inProgressJobs = await DbContext.Jobs.CountAsync(j => j.Status == JobStatus.InProgress);
        var cancelledJobs = await DbContext.Jobs.CountAsync(j => j.Status == JobStatus.Cancelled);
        var totalRevenue = await DbContext.Jobs
            .Where(j => j.Status == JobStatus.Completed)
            .SumAsync(j => j.Total);

        // Assert
        totalJobs.Should().Be(5); // 2 seeded + 3 additional
        scheduledJobs.Should().Be(2); // 2 from seeded data
        completedJobs.Should().Be(1);
        inProgressJobs.Should().Be(1);
        cancelledJobs.Should().Be(1);
        totalRevenue.Should().Be(150.00m);
    }

    [Fact]
    public async Task QueryJobsByDateRange_ShouldWorkCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();

        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(7);

        // Act
        var jobsInRange = await DbContext.Jobs
            .Where(j => j.ScheduledDate.HasValue &&
                       j.ScheduledDate.Value >= startDate &&
                       j.ScheduledDate.Value <= endDate &&
                       j.Status != JobStatus.Cancelled)
            .OrderBy(j => j.ScheduledDate)
            .ToListAsync();

        // Assert
        jobsInRange.Should().HaveCount(2);
        jobsInRange[0].Title.Should().Be("AC Repair");
        jobsInRange[1].Title.Should().Be("Furnace Maintenance");
    }

    [Fact]
    public async Task CustomerWithMultipleAssets_ShouldLoadCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();

        // Add another asset for customer 1
        var additionalAsset = new Asset
        {
            Serial = "XYZ789",
            Model = "Water Heater Model Z",
            Brand = "Heat Inc",
            CustomerId = 1, // Same customer as first asset
            Condition = AssetCondition.Good,
            EquipmentType = EquipmentType.HotWaterHeater,
            FuelType = FuelType.Electric,
            InstallDate = DateTime.Today.AddYears(-3),
            WarrantyStartDate = DateTime.Today.AddYears(-3),
            WarrantyTermYears = 8
        };

        await DbContext.Assets.AddAsync(additionalAsset);
        await DbContext.SaveChangesAsync();

        // Act
        var customerWithAssets = await DbContext.Customers
            .Include(c => c.Assets)
            .FirstOrDefaultAsync(c => c.Id == 1);

        // Assert
        customerWithAssets.Should().NotBeNull();
        customerWithAssets!.Assets.Should().HaveCount(2);
        customerWithAssets.Assets.Should().Contain(a => a.Serial == "ABC123");
        customerWithAssets.Assets.Should().Contain(a => a.Serial == "XYZ789");
    }

    [Fact]
    public async Task JobWithSiteInformation_ShouldLoadCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();

        // Create a site
        var site = new Site
        {
            Address = "123 Main St",
            City = "Anytown",
            State = "CA",
            ZipCode = "12345",
            CustomerId = 1
        };

        await DbContext.Sites.AddAsync(site);
        await DbContext.SaveChangesAsync();

        // Update job to reference the site
        var job = await DbContext.Jobs.FindAsync(1);
        job!.SiteId = site.Id;
        await DbContext.SaveChangesAsync();

        // Act
        var jobWithSite = await DbContext.Jobs
            .Include(j => j.Site)
            .Include(j => j.Customer)
            .FirstOrDefaultAsync(j => j.Id == 1);

        // Assert
        jobWithSite.Should().NotBeNull();
        jobWithSite!.Site.Should().NotBeNull();
        jobWithSite.Site.Address.Should().Be("123 Main St");
        jobWithSite.Customer.Should().NotBeNull();
        jobWithSite.Customer.Name.Should().Be("John Smith");
    }
}