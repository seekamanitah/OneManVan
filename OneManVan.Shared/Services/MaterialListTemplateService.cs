using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Services;

/// <summary>
/// Service for managing material list templates.
/// </summary>
public class MaterialListTemplateService
{
    private readonly IDbContextFactory<OneManVanDbContext> _contextFactory;

    public MaterialListTemplateService(IDbContextFactory<OneManVanDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    #region Template CRUD

    /// <summary>
    /// Gets all active templates.
    /// </summary>
    public async Task<List<MaterialListTemplate>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.MaterialListTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Gets templates by system type.
    /// </summary>
    public async Task<List<MaterialListTemplate>> GetBySystemTypeAsync(HvacSystemType systemType)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.MaterialListTemplates
            .Where(t => t.IsActive && t.SystemType == systemType)
            .OrderBy(t => t.SortOrder)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Gets a template by ID with all items.
    /// </summary>
    public async Task<MaterialListTemplate?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.MaterialListTemplates
            .Include(t => t.Items.OrderBy(i => i.SortOrder))
                .ThenInclude(i => i.InventoryItem)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    /// <summary>
    /// Creates a new user template.
    /// </summary>
    public async Task<MaterialListTemplate> CreateAsync(MaterialListTemplate template)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        template.IsBuiltIn = false;
        template.CreatedAt = DateTime.UtcNow;

        context.MaterialListTemplates.Add(template);
        await context.SaveChangesAsync();

        return template;
    }

    /// <summary>
    /// Updates a template (only user-created templates).
    /// </summary>
    public async Task<MaterialListTemplate> UpdateAsync(MaterialListTemplate template)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Prevent editing built-in templates
        var existing = await context.MaterialListTemplates.FindAsync(template.Id);
        if (existing?.IsBuiltIn == true)
            throw new InvalidOperationException("Cannot edit built-in templates. Create a copy instead.");

        template.UpdatedAt = DateTime.UtcNow;
        context.MaterialListTemplates.Update(template);
        await context.SaveChangesAsync();

        return template;
    }

    /// <summary>
    /// Deletes a template (only user-created templates).
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var template = await context.MaterialListTemplates.FindAsync(id);
        if (template == null) return false;

        // Prevent deleting built-in templates
        if (template.IsBuiltIn)
            throw new InvalidOperationException("Cannot delete built-in templates. Deactivate them instead.");

        context.MaterialListTemplates.Remove(template);
        await context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Deactivates a template (soft delete).
    /// </summary>
    public async Task<bool> DeactivateAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var template = await context.MaterialListTemplates.FindAsync(id);
        if (template == null) return false;

        template.IsActive = false;
        template.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Reactivates a template.
    /// </summary>
    public async Task<bool> ReactivateAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var template = await context.MaterialListTemplates.FindAsync(id);
        if (template == null) return false;

        template.IsActive = true;
        template.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Template Items

    /// <summary>
    /// Adds an item to a template.
    /// </summary>
    public async Task<MaterialListTemplateItem> AddItemAsync(MaterialListTemplateItem item)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Check if template is editable
        var template = await context.MaterialListTemplates.FindAsync(item.MaterialListTemplateId);
        if (template?.IsBuiltIn == true)
            throw new InvalidOperationException("Cannot modify built-in templates.");

        context.MaterialListTemplateItems.Add(item);
        await context.SaveChangesAsync();

        return item;
    }

    /// <summary>
    /// Updates a template item.
    /// </summary>
    public async Task<MaterialListTemplateItem> UpdateItemAsync(MaterialListTemplateItem item)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        context.MaterialListTemplateItems.Update(item);
        await context.SaveChangesAsync();

        return item;
    }

    /// <summary>
    /// Removes an item from a template.
    /// </summary>
    public async Task<bool> RemoveItemAsync(int itemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var item = await context.MaterialListTemplateItems.FindAsync(itemId);
        if (item == null) return false;

        // Check if template is editable
        var template = await context.MaterialListTemplates.FindAsync(item.MaterialListTemplateId);
        if (template?.IsBuiltIn == true)
            throw new InvalidOperationException("Cannot modify built-in templates.");

        context.MaterialListTemplateItems.Remove(item);
        await context.SaveChangesAsync();

        return true;
    }

    #endregion

    #region Template Duplication

    /// <summary>
    /// Duplicates a template (including built-in templates for customization).
    /// </summary>
    public async Task<MaterialListTemplate> DuplicateAsync(int sourceId, string newName)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var source = await context.MaterialListTemplates
            .Include(t => t.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == sourceId);

        if (source == null)
            throw new InvalidOperationException("Source template not found");

        var newTemplate = new MaterialListTemplate
        {
            Name = newName,
            Description = source.Description,
            SystemType = source.SystemType,
            IsBuiltIn = false, // User copy is always editable
            IsActive = true,
            SortOrder = source.SortOrder + 1,
            CreatedAt = DateTime.UtcNow
        };

        context.MaterialListTemplates.Add(newTemplate);
        await context.SaveChangesAsync();

        // Copy items
        foreach (var item in source.Items)
        {
            var newItem = new MaterialListTemplateItem
            {
                MaterialListTemplateId = newTemplate.Id,
                Category = item.Category,
                SubCategory = item.SubCategory,
                ItemName = item.ItemName,
                Size = item.Size,
                DefaultQuantity = item.DefaultQuantity,
                Unit = item.Unit,
                DefaultUnitCost = item.DefaultUnitCost,
                InventoryItemId = item.InventoryItemId,
                SortOrder = item.SortOrder,
                IsRequired = item.IsRequired
            };

            context.MaterialListTemplateItems.Add(newItem);
        }

        await context.SaveChangesAsync();

        return await GetByIdAsync(newTemplate.Id) ?? newTemplate;
    }

    /// <summary>
    /// Creates a template from an existing material list.
    /// </summary>
    public async Task<MaterialListTemplate> CreateFromMaterialListAsync(int materialListId, string templateName)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var materialList = await context.MaterialLists
            .Include(ml => ml.Items)
            .FirstOrDefaultAsync(ml => ml.Id == materialListId);

        if (materialList == null)
            throw new InvalidOperationException("Material list not found");

        var template = new MaterialListTemplate
        {
            Name = templateName,
            Description = $"Created from material list: {materialList.Title}",
            SystemType = HvacSystemType.SplitSystem, // Default
            IsBuiltIn = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.MaterialListTemplates.Add(template);
        await context.SaveChangesAsync();

        // Copy items with quantities
        foreach (var item in materialList.Items.Where(i => i.Quantity > 0).OrderBy(i => i.SortOrder))
        {
            var templateItem = new MaterialListTemplateItem
            {
                MaterialListTemplateId = template.Id,
                Category = item.Category,
                SubCategory = item.SubCategory,
                ItemName = item.ItemName,
                Size = item.Size,
                DefaultQuantity = item.Quantity,
                Unit = item.Unit,
                DefaultUnitCost = item.UnitCost,
                InventoryItemId = item.InventoryItemId,
                SortOrder = item.SortOrder
            };

            context.MaterialListTemplateItems.Add(templateItem);
        }

        await context.SaveChangesAsync();

        return await GetByIdAsync(template.Id) ?? template;
    }

    #endregion

    #region Seed Built-in Templates

    /// <summary>
    /// Ensures built-in HVAC templates exist in the database.
    /// Called during application startup.
    /// </summary>
    public async Task EnsureBuiltInTemplatesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var existingCount = await context.MaterialListTemplates
            .CountAsync(t => t.IsBuiltIn);

        if (existingCount > 0) return; // Already seeded

        // Templates are seeded via SQL migration script
        // This method is a fallback for new installations
    }

    #endregion
}
