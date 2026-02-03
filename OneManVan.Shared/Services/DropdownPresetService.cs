using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Shared.Services;

/// <summary>
/// Service for managing configurable dropdown presets.
/// Provides CRUD operations and category-specific queries.
/// </summary>
public class DropdownPresetService
{
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;

    public DropdownPresetService(IDbContextFactory<OneManVanDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Gets all active presets for a specific category.
    /// </summary>
    public async Task<List<DropdownPreset>> GetPresetsAsync(string category)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.DropdownPresets
            .Where(p => p.Category == category && p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.DisplayValue)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Gets all presets for a category (including inactive) for admin management.
    /// </summary>
    public async Task<List<DropdownPreset>> GetAllPresetsAsync(string category)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.DropdownPresets
            .Where(p => p.Category == category)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.DisplayValue)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Gets display values only for quick dropdown population.
    /// </summary>
    public async Task<List<string>> GetDisplayValuesAsync(string category)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.DropdownPresets
            .Where(p => p.Category == category && p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.DisplayValue)
            .Select(p => p.DisplayValue)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Gets all distinct categories in the system.
    /// </summary>
    public async Task<List<string>> GetCategoriesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.DropdownPresets
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Adds a new preset value.
    /// </summary>
    public async Task<DropdownPreset> AddPresetAsync(string category, string displayValue, string? storedValue = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Get max sort order for category
        var maxOrder = await db.DropdownPresets
            .Where(p => p.Category == category)
            .MaxAsync(p => (int?)p.SortOrder) ?? 0;

        var preset = new DropdownPreset
        {
            Category = category,
            DisplayValue = displayValue,
            StoredValue = storedValue,
            SortOrder = maxOrder + 1,
            IsActive = true,
            IsSystemDefault = false,
            CreatedAt = DateTime.UtcNow
        };

        db.DropdownPresets.Add(preset);
        await db.SaveChangesAsync();
        return preset;
    }

    /// <summary>
    /// Updates an existing preset.
    /// </summary>
    public async Task<bool> UpdatePresetAsync(int id, string displayValue, string? storedValue, int sortOrder, bool isActive)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var preset = await db.DropdownPresets.FindAsync(id);
        if (preset == null) return false;

        preset.DisplayValue = displayValue;
        preset.StoredValue = storedValue;
        preset.SortOrder = sortOrder;
        preset.IsActive = isActive;

        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Deletes a preset (only non-system defaults).
    /// </summary>
    public async Task<bool> DeletePresetAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var preset = await db.DropdownPresets.FindAsync(id);
        if (preset == null || preset.IsSystemDefault) return false;

        db.DropdownPresets.Remove(preset);
        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Toggles active status of a preset.
    /// </summary>
    public async Task<bool> ToggleActiveAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var preset = await db.DropdownPresets.FindAsync(id);
        if (preset == null) return false;

        preset.IsActive = !preset.IsActive;
        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Reorders presets within a category.
    /// </summary>
    public async Task ReorderPresetsAsync(string category, List<int> orderedIds)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var presets = await db.DropdownPresets
            .Where(p => p.Category == category)
            .ToListAsync();

        for (int i = 0; i < orderedIds.Count; i++)
        {
            var preset = presets.FirstOrDefault(p => p.Id == orderedIds[i]);
            if (preset != null)
            {
                preset.SortOrder = i + 1;
            }
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds default presets if the table is empty.
    /// Called on application startup.
    /// </summary>
    public async Task SeedDefaultsIfEmptyAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        
        if (await db.DropdownPresets.AnyAsync())
            return;

        var defaults = new List<DropdownPreset>
        {
            // Manufacturers
            new() { Category = PresetCategories.Manufacturer, DisplayValue = "Carrier", SortOrder = 1, IsSystemDefault = true },
            new() { Category = PresetCategories.Manufacturer, DisplayValue = "Trane", SortOrder = 2, IsSystemDefault = true },
            new() { Category = PresetCategories.Manufacturer, DisplayValue = "Lennox", SortOrder = 3, IsSystemDefault = true },
            new() { Category = PresetCategories.Manufacturer, DisplayValue = "Rheem", SortOrder = 4, IsSystemDefault = true },
            new() { Category = PresetCategories.Manufacturer, DisplayValue = "Goodman", SortOrder = 5, IsSystemDefault = true },
            new() { Category = PresetCategories.Manufacturer, DisplayValue = "Daikin", SortOrder = 6, IsSystemDefault = true },
            new() { Category = PresetCategories.Manufacturer, DisplayValue = "York", SortOrder = 7, IsSystemDefault = true },
            new() { Category = PresetCategories.Manufacturer, DisplayValue = "American Standard", SortOrder = 8, IsSystemDefault = true },
            new() { Category = PresetCategories.Manufacturer, DisplayValue = "Bryant", SortOrder = 9, IsSystemDefault = true },
            new() { Category = PresetCategories.Manufacturer, DisplayValue = "Mitsubishi", SortOrder = 10, IsSystemDefault = true },

            // Payment Terms
            new() { Category = PresetCategories.PaymentTerms, DisplayValue = "Due on Receipt", StoredValue = "0", SortOrder = 1, IsSystemDefault = true },
            new() { Category = PresetCategories.PaymentTerms, DisplayValue = "Net 15", StoredValue = "15", SortOrder = 2, IsSystemDefault = true },
            new() { Category = PresetCategories.PaymentTerms, DisplayValue = "Net 30", StoredValue = "30", SortOrder = 3, IsSystemDefault = true },
            new() { Category = PresetCategories.PaymentTerms, DisplayValue = "Net 45", StoredValue = "45", SortOrder = 4, IsSystemDefault = true },
            new() { Category = PresetCategories.PaymentTerms, DisplayValue = "Net 60", StoredValue = "60", SortOrder = 5, IsSystemDefault = true },

            // Warranty Lengths
            new() { Category = PresetCategories.WarrantyLength, DisplayValue = "1 Year", StoredValue = "1", SortOrder = 1, IsSystemDefault = true },
            new() { Category = PresetCategories.WarrantyLength, DisplayValue = "2 Years", StoredValue = "2", SortOrder = 2, IsSystemDefault = true },
            new() { Category = PresetCategories.WarrantyLength, DisplayValue = "5 Years", StoredValue = "5", SortOrder = 3, IsSystemDefault = true },
            new() { Category = PresetCategories.WarrantyLength, DisplayValue = "10 Years", StoredValue = "10", SortOrder = 4, IsSystemDefault = true },
        };

        db.DropdownPresets.AddRange(defaults);
        await db.SaveChangesAsync();
    }
}
