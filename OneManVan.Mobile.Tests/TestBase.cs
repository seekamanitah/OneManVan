using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Tests;

/// <summary>
/// Base class for all tests that provides common setup and utilities.
/// Focuses on testing business logic and data operations.
/// </summary>
public abstract class TestBase : IDisposable
{
    protected readonly IDbContextFactory<OneManVanDbContext> DbFactory;
    protected readonly OneManVanDbContext DbContext;

    protected TestBase()
    {
        // Setup in-memory database for testing
        var options = new DbContextOptionsBuilder<OneManVanDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        DbContext = new OneManVanDbContext(options);
        DbFactory = new TestDbContextFactory(DbContext);

        // Ensure database is created
        DbContext.Database.EnsureCreated();
    }

    /// <summary>
    /// Creates test data for common entities.
    /// </summary>
    protected async Task SeedTestDataAsync()
    {
        // Create test customers
        var customers = new[]
        {
            new Customer
            {
                Name = "John Smith",
                Email = "john@example.com",
                Phone = "(555) 123-4567"
            },
            new Customer
            {
                Name = "Jane Doe",
                Email = "jane@example.com",
                Phone = "(555) 987-6543"
            }
        };

        await DbContext.Customers.AddRangeAsync(customers);

        // Create test assets
        var assets = new[]
        {
            new Asset
            {
                Serial = "ABC123",
                Model = "AC Unit Model X",
                CustomerId = 1,
                Condition = AssetCondition.Good,
                EquipmentType = EquipmentType.AirConditioner,
                FuelType = FuelType.Electric,
                InstallDate = DateTime.Today.AddYears(-2),
                WarrantyStartDate = DateTime.Today.AddYears(-2),
                WarrantyTermYears = 10
            },
            new Asset
            {
                Serial = "DEF456",
                Model = "Furnace Model Y",
                CustomerId = 2,
                Condition = AssetCondition.Excellent,
                EquipmentType = EquipmentType.GasFurnace,
                FuelType = FuelType.NaturalGas,
                InstallDate = DateTime.Today.AddYears(-1),
                WarrantyStartDate = DateTime.Today.AddYears(-1),
                WarrantyTermYears = 5
            }
        };

        await DbContext.Assets.AddRangeAsync(assets);

        // Create test jobs
        var jobs = new[]
        {
            new Job
            {
                Title = "AC Repair",
                Description = "Fix broken air conditioner",
                CustomerId = 1,
                ScheduledDate = DateTime.Today.AddDays(1),
                ArrivalWindowStart = new TimeSpan(9, 0, 0),
                ArrivalWindowEnd = new TimeSpan(11, 0, 0),
                Priority = JobPriority.Normal,
                Status = JobStatus.Scheduled,
                EstimatedHours = 2.0m,
                Total = 150.00m
            },
            new Job
            {
                Title = "Furnace Maintenance",
                Description = "Annual furnace checkup",
                CustomerId = 2,
                ScheduledDate = DateTime.Today.AddDays(2),
                ArrivalWindowStart = new TimeSpan(13, 0, 0),
                ArrivalWindowEnd = new TimeSpan(15, 0, 0),
                Priority = JobPriority.Low,
                Status = JobStatus.Scheduled,
                EstimatedHours = 1.5m,
                Total = 85.00m
            }
        };

        await DbContext.Jobs.AddRangeAsync(jobs);
        await DbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        DbContext?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Simple DbContext factory for testing
    /// </summary>
    private class TestDbContextFactory : IDbContextFactory<OneManVanDbContext>
    {
        private readonly OneManVanDbContext _context;

        public TestDbContextFactory(OneManVanDbContext context)
        {
            _context = context;
        }

        public OneManVanDbContext CreateDbContext()
        {
            return _context;
        }
    }
}