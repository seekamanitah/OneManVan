using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Services;

/// <summary>
/// Service for managing material lists and related operations.
/// </summary>
public class MaterialListService
{
    private readonly IDbContextFactory<OneManVanDbContext> _contextFactory;

    public MaterialListService(IDbContextFactory<OneManVanDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    #region Material List CRUD

    /// <summary>
    /// Gets all material lists with basic info.
    /// </summary>
    public async Task<List<MaterialList>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.MaterialLists
            .Include(ml => ml.Customer)
            .Include(ml => ml.Site)
            .OrderByDescending(ml => ml.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Gets material lists filtered by status.
    /// </summary>
    public async Task<List<MaterialList>> GetByStatusAsync(MaterialListStatus status)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.MaterialLists
            .Include(ml => ml.Customer)
            .Include(ml => ml.Site)
            .Where(ml => ml.Status == status)
            .OrderByDescending(ml => ml.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Gets material lists for a specific customer.
    /// </summary>
    public async Task<List<MaterialList>> GetByCustomerAsync(int customerId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.MaterialLists
            .Include(ml => ml.Site)
            .Where(ml => ml.CustomerId == customerId)
            .OrderByDescending(ml => ml.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Gets material lists for a specific site.
    /// </summary>
    public async Task<List<MaterialList>> GetBySiteAsync(int siteId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.MaterialLists
            .Include(ml => ml.Customer)
            .Where(ml => ml.SiteId == siteId)
            .OrderByDescending(ml => ml.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Gets a material list by ID with all related data.
    /// </summary>
    public async Task<MaterialList?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.MaterialLists
            .Include(ml => ml.Customer)
            .Include(ml => ml.Site)
            .Include(ml => ml.Systems)
            .Include(ml => ml.Items)
                .ThenInclude(i => i.InventoryItem)
            .FirstOrDefaultAsync(ml => ml.Id == id);
    }

    /// <summary>
    /// Creates a new material list.
    /// </summary>
    public async Task<MaterialList> CreateAsync(MaterialList materialList)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Generate list number
        var count = await context.MaterialLists.CountAsync() + 1;
        materialList.ListNumber = $"ML-{DateTime.Now.Year}-{count:D4}";
        materialList.CreatedAt = DateTime.UtcNow;

        // Create default system if none provided
        if (!materialList.Systems.Any())
        {
            materialList.Systems.Add(new MaterialListSystem
            {
                Name = "System 1",
                SortOrder = 1,
                SystemType = HvacSystemType.SplitSystem
            });
        }

        context.MaterialLists.Add(materialList);
        await context.SaveChangesAsync();

        return materialList;
    }

    /// <summary>
    /// Updates a material list.
    /// </summary>
    public async Task<MaterialList> UpdateAsync(MaterialList materialList)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        materialList.UpdatedAt = DateTime.UtcNow;
        context.MaterialLists.Update(materialList);
        await context.SaveChangesAsync();

        return materialList;
    }

    /// <summary>
    /// Deletes a material list.
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var materialList = await context.MaterialLists.FindAsync(id);
        if (materialList == null) return false;

        context.MaterialLists.Remove(materialList);
        await context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Finalizes a material list (locks it for editing with warning).
    /// </summary>
    public async Task<bool> FinalizeAsync(int id, string? finalizedBy = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var materialList = await context.MaterialLists.FindAsync(id);
        if (materialList == null) return false;

        materialList.Status = MaterialListStatus.Finalized;
        materialList.FinalizedAt = DateTime.UtcNow;
        materialList.FinalizedBy = finalizedBy;
        materialList.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Reopens a finalized list for editing.
    /// </summary>
    public async Task<bool> ReopenAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var materialList = await context.MaterialLists.FindAsync(id);
        if (materialList == null) return false;

        materialList.Status = MaterialListStatus.Draft;
        materialList.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Systems (Multi-zone)

    /// <summary>
    /// Adds a new system/zone to a material list.
    /// </summary>
    public async Task<MaterialListSystem> AddSystemAsync(int materialListId, string name)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var existingCount = await context.MaterialListSystems
            .CountAsync(s => s.MaterialListId == materialListId);

        var system = new MaterialListSystem
        {
            MaterialListId = materialListId,
            Name = name,
            SortOrder = existingCount + 1,
            SystemType = HvacSystemType.SplitSystem
        };

        context.MaterialListSystems.Add(system);
        await context.SaveChangesAsync();

        return system;
    }

    /// <summary>
    /// Updates a system/zone.
    /// </summary>
    public async Task<MaterialListSystem> UpdateSystemAsync(MaterialListSystem system)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        context.MaterialListSystems.Update(system);
        await context.SaveChangesAsync();

        return system;
    }

    /// <summary>
    /// Removes a system/zone (moves items to unassigned).
    /// </summary>
    public async Task<bool> RemoveSystemAsync(int systemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var system = await context.MaterialListSystems
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == systemId);

        if (system == null) return false;

        // Move items to unassigned (null system)
        foreach (var item in system.Items)
        {
            item.MaterialListSystemId = null;
        }

        context.MaterialListSystems.Remove(system);
        await context.SaveChangesAsync();

        return true;
    }

    #endregion

    #region Items

    /// <summary>
    /// Adds an item to a material list.
    /// </summary>
    public async Task<MaterialListItem> AddItemAsync(MaterialListItem item)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // If inventory item linked, get cost
        if (item.InventoryItemId.HasValue && !item.IsCostOverridden)
        {
            var invItem = await context.InventoryItems.FindAsync(item.InventoryItemId);
            if (invItem != null)
            {
                item.UnitCost = invItem.Cost;
                item.IsFromInventory = true;
            }
        }

        context.MaterialListItems.Add(item);
        await context.SaveChangesAsync();

        // Recalculate totals
        await RecalculateTotalsAsync(item.MaterialListId);

        return item;
    }

    /// <summary>
    /// Updates an item.
    /// </summary>
    public async Task<MaterialListItem> UpdateItemAsync(MaterialListItem item)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        context.MaterialListItems.Update(item);
        await context.SaveChangesAsync();

        // Recalculate totals
        await RecalculateTotalsAsync(item.MaterialListId);

        return item;
    }

    /// <summary>
    /// Removes an item.
    /// </summary>
    public async Task<bool> RemoveItemAsync(int itemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var item = await context.MaterialListItems.FindAsync(itemId);
        if (item == null) return false;

        var materialListId = item.MaterialListId;

        context.MaterialListItems.Remove(item);
        await context.SaveChangesAsync();

        // Recalculate totals
        await RecalculateTotalsAsync(materialListId);

        return true;
    }

    /// <summary>
    /// Overrides the cost for an item.
    /// </summary>
    public async Task<bool> OverrideItemCostAsync(int itemId, decimal newCost)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var item = await context.MaterialListItems.FindAsync(itemId);
        if (item == null) return false;

        if (!item.IsCostOverridden)
        {
            item.OriginalCost = item.UnitCost;
        }

        item.UnitCost = newCost;
        item.IsCostOverridden = true;

        await context.SaveChangesAsync();

        // Recalculate totals
        await RecalculateTotalsAsync(item.MaterialListId);

        return true;
    }

    /// <summary>
    /// Resets an item cost to original/inventory cost.
    /// </summary>
    public async Task<bool> ResetItemCostAsync(int itemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var item = await context.MaterialListItems
            .Include(i => i.InventoryItem)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null) return false;

        if (item.OriginalCost.HasValue)
        {
            item.UnitCost = item.OriginalCost.Value;
        }
        else if (item.InventoryItem != null)
        {
            item.UnitCost = item.InventoryItem.Cost;
        }

        item.IsCostOverridden = false;
        item.OriginalCost = null;

        await context.SaveChangesAsync();

        // Recalculate totals
        await RecalculateTotalsAsync(item.MaterialListId);

        return true;
    }

    #endregion

    #region Calculations

    /// <summary>
    /// Recalculates total costs for a material list.
    /// </summary>
    public async Task RecalculateTotalsAsync(int materialListId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var materialList = await context.MaterialLists
            .Include(ml => ml.Items)
            .FirstOrDefaultAsync(ml => ml.Id == materialListId);

        if (materialList == null) return;

        materialList.TotalMaterialCost = materialList.Items.Sum(i => i.Quantity * i.UnitCost);
        materialList.TotalWithMarkup = materialList.TotalMaterialCost * (1 + materialList.MarkupPercent / 100);
        materialList.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates markup percentage and recalculates total.
    /// </summary>
    public async Task<bool> SetMarkupAsync(int materialListId, decimal markupPercent)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var materialList = await context.MaterialLists
            .Include(ml => ml.Items)
            .FirstOrDefaultAsync(ml => ml.Id == materialListId);

        if (materialList == null) return false;

        materialList.MarkupPercent = markupPercent;
        materialList.TotalMaterialCost = materialList.Items.Sum(i => i.Quantity * i.UnitCost);
        materialList.TotalWithMarkup = materialList.TotalMaterialCost * (1 + markupPercent / 100);
        materialList.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Template Application

    /// <summary>
    /// Applies a template to a material list, adding all template items.
    /// </summary>
    public async Task<int> ApplyTemplateAsync(int materialListId, int templateId, int? systemId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var template = await context.MaterialListTemplates
            .Include(t => t.Items)
                .ThenInclude(i => i.InventoryItem)
            .FirstOrDefaultAsync(t => t.Id == templateId);

        if (template == null) return 0;

        var itemsAdded = 0;

        foreach (var templateItem in template.Items.OrderBy(i => i.SortOrder))
        {
            var item = new MaterialListItem
            {
                MaterialListId = materialListId,
                MaterialListSystemId = systemId,
                Category = templateItem.Category,
                SubCategory = templateItem.SubCategory,
                ItemName = templateItem.ItemName,
                Size = templateItem.Size,
                Quantity = templateItem.DefaultQuantity,
                Unit = templateItem.Unit,
                UnitCost = templateItem.DefaultUnitCost > 0 
                    ? templateItem.DefaultUnitCost 
                    : (templateItem.InventoryItem?.Cost ?? 0),
                InventoryItemId = templateItem.InventoryItemId,
                IsFromInventory = templateItem.InventoryItemId.HasValue,
                SortOrder = templateItem.SortOrder
            };

            context.MaterialListItems.Add(item);
            itemsAdded++;
        }

        await context.SaveChangesAsync();

        // Recalculate totals
        await RecalculateTotalsAsync(materialListId);

        return itemsAdded;
    }

    #endregion

    #region Duplication

    /// <summary>
    /// Duplicates a material list.
    /// </summary>
    public async Task<MaterialList> DuplicateAsync(int sourceId, string newTitle)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var source = await context.MaterialLists
            .Include(ml => ml.Systems)
            .Include(ml => ml.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(ml => ml.Id == sourceId);

        if (source == null)
            throw new InvalidOperationException("Source material list not found");

        // Create new list
        var count = await context.MaterialLists.CountAsync() + 1;
        var newList = new MaterialList
        {
            ListNumber = $"ML-{DateTime.Now.Year}-{count:D4}",
            Title = newTitle,
            CustomerId = source.CustomerId,
            SiteId = source.SiteId,
            SquareFootage = source.SquareFootage,
            Zones = source.Zones,
            Stories = source.Stories,
            Status = MaterialListStatus.Draft,
            MarkupPercent = source.MarkupPercent,
            Notes = source.Notes,
            InternalNotes = source.InternalNotes,
            CreatedAt = DateTime.UtcNow
        };

        context.MaterialLists.Add(newList);
        await context.SaveChangesAsync();

        // Map old system IDs to new ones
        var systemIdMap = new Dictionary<int, int>();

        foreach (var system in source.Systems.OrderBy(s => s.SortOrder))
        {
            var newSystem = new MaterialListSystem
            {
                MaterialListId = newList.Id,
                Name = system.Name,
                SortOrder = system.SortOrder,
                SystemType = system.SystemType,
                IsTrunkSystem = system.IsTrunkSystem,
                PlenumMaterial = system.PlenumMaterial,
                MainTrunkSize = system.MainTrunkSize,
                EquipmentModel = system.EquipmentModel,
                Tonnage = system.Tonnage,
                SeerRating = system.SeerRating,
                BtuRating = system.BtuRating,
                SquareFootage = system.SquareFootage,
                Notes = system.Notes
            };

            context.MaterialListSystems.Add(newSystem);
            await context.SaveChangesAsync();

            systemIdMap[system.Id] = newSystem.Id;
        }

        // Copy items
        foreach (var item in source.Items.OrderBy(i => i.SortOrder))
        {
            var newItem = new MaterialListItem
            {
                MaterialListId = newList.Id,
                MaterialListSystemId = item.MaterialListSystemId.HasValue && systemIdMap.ContainsKey(item.MaterialListSystemId.Value)
                    ? systemIdMap[item.MaterialListSystemId.Value]
                    : null,
                Category = item.Category,
                SubCategory = item.SubCategory,
                ItemName = item.ItemName,
                Size = item.Size,
                Description = item.Description,
                Quantity = item.Quantity,
                Unit = item.Unit,
                UnitCost = item.UnitCost,
                IsCostOverridden = item.IsCostOverridden,
                OriginalCost = item.OriginalCost,
                InventoryItemId = item.InventoryItemId,
                IsFromInventory = item.IsFromInventory,
                IsCustomItem = item.IsCustomItem,
                SortOrder = item.SortOrder,
                Notes = item.Notes
            };

            context.MaterialListItems.Add(newItem);
        }

        await context.SaveChangesAsync();

        // Recalculate totals
        await RecalculateTotalsAsync(newList.Id);

        return await GetByIdAsync(newList.Id) ?? newList;
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Gets counts by status.
    /// </summary>
    public async Task<Dictionary<MaterialListStatus, int>> GetCountsByStatusAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.MaterialLists
            .GroupBy(ml => ml.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);
    }

    #endregion
}
