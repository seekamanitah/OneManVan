using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Services;

/// <summary>
/// Service for looking up and selecting inventory items from jobs.
/// Supports searching, filtering by category, and compatibility matching.
/// </summary>
public class InventoryLookupService
{
    private readonly OneManVanDbContext _dbContext;

    public InventoryLookupService(OneManVanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Searches inventory items by name, SKU, or part number.
    /// </summary>
    public async Task<List<InventoryItem>> SearchAsync(string? searchText, int maxResults = 20)
    {
        var query = _dbContext.InventoryItems
            .Where(i => i.IsActive && i.QuantityOnHand > 0);

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var search = searchText.ToLowerInvariant();
            query = query.Where(i =>
                i.Name.ToLower().Contains(search) ||
                (i.Sku != null && i.Sku.ToLower().Contains(search)) ||
                (i.PartNumber != null && i.PartNumber.ToLower().Contains(search)) ||
                (i.Description != null && i.Description.ToLower().Contains(search)));
        }

        return await query
            .OrderBy(i => i.Name)
            .Take(maxResults)
            .ToListAsync();
    }

    /// <summary>
    /// Gets items by category.
    /// </summary>
    public async Task<List<InventoryItem>> GetByCategoryAsync(InventoryCategory category)
    {
        return await _dbContext.InventoryItems
            .Where(i => i.IsActive && i.Category == category && i.QuantityOnHand > 0)
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Gets commonly used items (based on usage in jobs).
    /// </summary>
    public async Task<List<InventoryItem>> GetFrequentlyUsedAsync(int limit = 10)
    {
        return await _dbContext.InventoryItems
            .Where(i => i.IsActive && i.QuantityOnHand > 0)
            .OrderByDescending(i => i.QuantityOnHand)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Gets items compatible with an asset's specifications.
    /// </summary>
    public async Task<List<InventoryItem>> GetCompatibleWithAssetAsync(Asset? asset)
    {
        if (asset == null)
        {
            return await GetFrequentlyUsedAsync();
        }

        var query = _dbContext.InventoryItems
            .Where(i => i.IsActive && i.QuantityOnHand > 0);

        // Filter by refrigerant type if asset has one
        if (asset.RefrigerantType != RefrigerantType.Unknown)
        {
            var refrigerantName = asset.RefrigerantType.ToString();
            query = query.Where(i => 
                i.RefrigerantType == null || 
                i.RefrigerantType.Contains(refrigerantName));
        }

        // Filter by fuel type if applicable
        if (asset.FuelType != FuelType.Electric)
        {
            query = query.Where(i =>
                i.FuelTypeCompatibility == null ||
                i.FuelTypeCompatibility == asset.FuelType);
        }

        // Filter by BTU capacity
        if (asset.TonnageX10 > 0)
        {
            var btu = (asset.TonnageX10 / 10.0) * 12000; // Rough BTU calculation
            query = query.Where(i =>
                (i.BtuMinCompatibility == null || i.BtuMinCompatibility <= btu) &&
                (i.BtuMaxCompatibility == null || i.BtuMaxCompatibility >= btu));
        }

        // Filter by filter size if asset has one
        if (!string.IsNullOrEmpty(asset.FilterSize))
        {
            query = query.Where(i =>
                i.FilterSize == null ||
                i.FilterSize == asset.FilterSize);
        }

        return await query
            .OrderBy(i => i.Name)
            .Take(50)
            .ToListAsync();
    }

    /// <summary>
    /// Deducts quantity from inventory.
    /// </summary>
    public async Task<bool> DeductInventoryAsync(int itemId, decimal quantity, int jobId, string? notes = null)
    {
        var item = await _dbContext.InventoryItems.FindAsync(itemId);
        if (item == null || item.QuantityOnHand < quantity)
        {
            return false;
        }

        var quantityBefore = item.QuantityOnHand;
        item.QuantityOnHand -= quantity;

        // Log the deduction
        var log = new InventoryLog
        {
            InventoryItemId = itemId,
            QuantityChange = -quantity,
            QuantityBefore = quantityBefore,
            QuantityAfter = item.QuantityOnHand,
            ChangeType = InventoryChangeType.UsedOnJob,
            JobId = jobId,
            Notes = notes,
            Timestamp = DateTime.UtcNow
        };

        _dbContext.InventoryLogs.Add(log);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Gets low stock alerts.
    /// </summary>
    public async Task<List<InventoryItem>> GetLowStockItemsAsync()
    {
        return await _dbContext.InventoryItems
            .Where(i => i.IsActive && i.QuantityOnHand <= i.ReorderPoint)
            .OrderBy(i => i.QuantityOnHand)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all categories with item counts.
    /// </summary>
    public async Task<Dictionary<InventoryCategory, int>> GetCategoryCountsAsync()
    {
        return await _dbContext.InventoryItems
            .Where(i => i.IsActive && i.QuantityOnHand > 0)
            .GroupBy(i => i.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Category, x => x.Count);
    }
}

/// <summary>
/// View model for displaying inventory items in selection lists.
/// </summary>
public class InventoryItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Sku { get; set; }
    public string? PartNumber { get; set; }
    public string Category { get; set; } = "";
    public decimal QuantityOnHand { get; set; }
    public string Unit { get; set; } = "ea";
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public bool IsLowStock { get; set; }
    
    public string DisplayName => string.IsNullOrEmpty(Sku) ? Name : $"{Name} ({Sku})";
    public string QuantityDisplay => $"{QuantityOnHand:F0} {Unit}";
    public string PriceDisplay => $"${Price:F2}";
    public string StockStatusIcon => IsLowStock ? "LOW" : "OK";
    public Color StockStatusColor => IsLowStock ? Colors.Orange : Colors.Green;

    public static InventoryItemViewModel FromModel(InventoryItem item)
    {
        return new InventoryItemViewModel
        {
            Id = item.Id,
            Name = item.Name,
            Sku = item.Sku,
            PartNumber = item.PartNumber,
            Category = item.Category.ToString(),
            QuantityOnHand = item.QuantityOnHand,
            Unit = item.Unit,
            Price = item.Price,
            Cost = item.Cost,
            IsLowStock = item.IsLowStock
        };
    }
}
