using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Services;

/// <summary>
/// Service to apply field enhancement migrations programmatically.
/// </summary>
public class FieldEnhancementMigrationService
{
    private readonly OneManVanDbContext _dbContext;

    public FieldEnhancementMigrationService(OneManVanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Applies all field enhancement migrations.
    /// </summary>
    public async Task ApplyMigrationsAsync()
    {
        try
        {
            // Ensure database is created
            await _dbContext.Database.EnsureCreatedAsync();

            // Apply pending migrations
            var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                await _dbContext.Database.MigrateAsync();
            }

            // Run data migrations
            await MigrateCustomerNamesAsync();
            await SeedDefaultDataAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to apply migrations: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Migrates existing Customer.Name to FirstName and LastName.
    /// </summary>
    private async Task MigrateCustomerNamesAsync()
    {
        var customers = await _dbContext.Customers
            .Where(c => c.FirstName == null && c.LastName == null && c.Name != null)
            .ToListAsync();

        foreach (var customer in customers)
        {
            var nameParts = customer.Name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            
            if (nameParts.Length == 2)
            {
                customer.FirstName = nameParts[0];
                customer.LastName = nameParts[1];
            }
            else if (nameParts.Length == 1)
            {
                customer.FirstName = nameParts[0];
                customer.LastName = string.Empty;
            }
        }

        if (customers.Any())
        {
            await _dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Seeds default data for custom field definitions and choices.
    /// </summary>
    private async Task SeedDefaultDataAsync()
    {
        // Check if Brand field definition exists
        var brandField = await _dbContext.CustomFieldDefinitions
            .FirstOrDefaultAsync(f => f.EntityType == "Asset" && f.FieldName == "Brand");

        if (brandField == null)
        {
            // Create Brand field definition
            brandField = new CustomFieldDefinition
            {
                EntityType = "Asset",
                FieldName = "Brand",
                DisplayName = "Brand",
                FieldType = "Dropdown",
                IsSystemField = true,
                DisplayOrder = 1,
                GroupName = "Equipment Info"
            };

            _dbContext.CustomFieldDefinitions.Add(brandField);
            await _dbContext.SaveChangesAsync();

            // Add brand choices
            var brandChoices = new List<CustomFieldChoice>
            {
                new() { FieldDefinitionId = brandField.Id, Value = "carrier", DisplayText = "Carrier", DisplayOrder = 1 },
                new() { FieldDefinitionId = brandField.Id, Value = "trane", DisplayText = "Trane", DisplayOrder = 2 },
                new() { FieldDefinitionId = brandField.Id, Value = "lennox", DisplayText = "Lennox", DisplayOrder = 3 },
                new() { FieldDefinitionId = brandField.Id, Value = "rheem", DisplayText = "Rheem", DisplayOrder = 4 },
                new() { FieldDefinitionId = brandField.Id, Value = "goodman", DisplayText = "Goodman", DisplayOrder = 5 },
                new() { FieldDefinitionId = brandField.Id, Value = "york", DisplayText = "York", DisplayOrder = 6 },
                new() { FieldDefinitionId = brandField.Id, Value = "daikin", DisplayText = "Daikin", DisplayOrder = 7 },
                new() { FieldDefinitionId = brandField.Id, Value = "mitsubishi", DisplayText = "Mitsubishi", DisplayOrder = 8 },
                new() { FieldDefinitionId = brandField.Id, Value = "fujitsu", DisplayText = "Fujitsu", DisplayOrder = 9 },
                new() { FieldDefinitionId = brandField.Id, Value = "bryant", DisplayText = "Bryant", DisplayOrder = 10 },
                new() { FieldDefinitionId = brandField.Id, Value = "american_standard", DisplayText = "American Standard", DisplayOrder = 11 },
                new() { FieldDefinitionId = brandField.Id, Value = "ruud", DisplayText = "Ruud", DisplayOrder = 12 },
                new() { FieldDefinitionId = brandField.Id, Value = "other", DisplayText = "Other", DisplayOrder = 99 }
            };

            _dbContext.CustomFieldChoices.AddRange(brandChoices);
            await _dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Checks if migrations need to be applied.
    /// </summary>
    public async Task<bool> NeedsMigrationAsync()
    {
        try
        {
            var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync();
            return pendingMigrations.Any();
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// Gets migration status information.
    /// </summary>
    public async Task<MigrationStatus> GetMigrationStatusAsync()
    {
        try
        {
            var appliedMigrations = await _dbContext.Database.GetAppliedMigrationsAsync();
            var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync();

            return new MigrationStatus
            {
                IsUpToDate = !pendingMigrations.Any(),
                AppliedCount = appliedMigrations.Count(),
                PendingCount = pendingMigrations.Count(),
                AppliedMigrations = appliedMigrations.ToList(),
                PendingMigrations = pendingMigrations.ToList()
            };
        }
        catch (Exception ex)
        {
            return new MigrationStatus
            {
                IsUpToDate = false,
                Error = ex.Message
            };
        }
    }
}

/// <summary>
/// Migration status information.
/// </summary>
public class MigrationStatus
{
    public bool IsUpToDate { get; set; }
    public int AppliedCount { get; set; }
    public int PendingCount { get; set; }
    public List<string> AppliedMigrations { get; set; } = [];
    public List<string> PendingMigrations { get; set; } = [];
    public string? Error { get; set; }
}
