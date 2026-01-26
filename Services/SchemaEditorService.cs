using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Services;

/// <summary>
/// Service for managing schema definitions (custom fields).
/// </summary>
public class SchemaEditorService
{
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    
    /// <summary>
    /// Gets all schema definitions for a specific entity type.
    /// </summary>
    public async Task<List<SchemaDefinition>> GetSchemaDefinitionsAsync(string entityType)
    {
        return await App.DbContext.SchemaDefinitions
            .Where(s => s.EntityType == entityType && s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.DisplayLabel)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all active schema definitions grouped by entity type.
    /// </summary>
    public async Task<Dictionary<string, List<SchemaDefinition>>> GetAllSchemaDefinitionsAsync()
    {
        var definitions = await App.DbContext.SchemaDefinitions
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.DisplayLabel)
            .ToListAsync();

        return definitions.GroupBy(d => d.EntityType)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Gets a schema definition by ID.
    /// </summary>
    public async Task<SchemaDefinition?> GetSchemaDefinitionAsync(int id)
    {
        return await App.DbContext.SchemaDefinitions.FindAsync(id);
    }

    /// <summary>
    /// Creates a new schema definition.
    /// </summary>
    public async Task<SchemaDefinition> CreateSchemaDefinitionAsync(SchemaDefinition definition)
    {
        // Validate unique field name per entity type
        var exists = await App.DbContext.SchemaDefinitions
            .AnyAsync(s => s.EntityType == definition.EntityType && 
                          s.FieldName == definition.FieldName &&
                          s.IsActive);

        if (exists)
        {
            throw new InvalidOperationException($"A field named '{definition.FieldName}' already exists for {definition.EntityType}.");
        }

        // Set display order to end
        var maxOrder = await App.DbContext.SchemaDefinitions
            .Where(s => s.EntityType == definition.EntityType)
            .MaxAsync(s => (int?)s.DisplayOrder) ?? 0;
        
        definition.DisplayOrder = maxOrder + 1;
        definition.CreatedAt = DateTime.UtcNow;

        App.DbContext.SchemaDefinitions.Add(definition);
        await App.DbContext.SaveChangesAsync();

        return definition;
    }

    /// <summary>
    /// Updates an existing schema definition.
    /// </summary>
    public async Task<SchemaDefinition> UpdateSchemaDefinitionAsync(SchemaDefinition definition)
    {
        var existing = await App.DbContext.SchemaDefinitions.FindAsync(definition.Id);
        if (existing == null)
        {
            throw new InvalidOperationException("Schema definition not found.");
        }

        // Check if field name changed and is now duplicate
        if (existing.FieldName != definition.FieldName)
        {
            var nameExists = await App.DbContext.SchemaDefinitions
                .AnyAsync(s => s.EntityType == definition.EntityType && 
                              s.FieldName == definition.FieldName &&
                              s.Id != definition.Id &&
                              s.IsActive);

            if (nameExists)
            {
                throw new InvalidOperationException($"A field named '{definition.FieldName}' already exists for {definition.EntityType}.");
            }
        }

        existing.FieldName = definition.FieldName;
        existing.DisplayLabel = definition.DisplayLabel;
        existing.FieldType = definition.FieldType;
        existing.EnumOptions = definition.EnumOptions;
        existing.DefaultValue = definition.DefaultValue;
        existing.IsRequired = definition.IsRequired;
        existing.Placeholder = definition.Placeholder;
        existing.Description = definition.Description;
        existing.MinValue = definition.MinValue;
        existing.MaxValue = definition.MaxValue;
        existing.MaxLength = definition.MaxLength;
        existing.DisplayOrder = definition.DisplayOrder;
        existing.ModifiedAt = DateTime.UtcNow;

        await App.DbContext.SaveChangesAsync();

        return existing;
    }

    /// <summary>
    /// Soft-deletes a schema definition.
    /// </summary>
    public async Task DeleteSchemaDefinitionAsync(int id)
    {
        var definition = await App.DbContext.SchemaDefinitions.FindAsync(id);
        if (definition == null)
        {
            throw new InvalidOperationException("Schema definition not found.");
        }

        // Soft delete
        definition.IsActive = false;
        definition.ModifiedAt = DateTime.UtcNow;

        await App.DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Permanently deletes a schema definition and all its values.
    /// </summary>
    public async Task PermanentDeleteSchemaDefinitionAsync(int id)
    {
        var definition = await App.DbContext.SchemaDefinitions.FindAsync(id);
        if (definition == null)
        {
            throw new InvalidOperationException("Schema definition not found.");
        }

        // Delete all custom field values for this definition
        var customFields = await App.DbContext.CustomFields
            .Where(cf => cf.EntityType == definition.EntityType && cf.Key == definition.FieldName)
            .ToListAsync();

        App.DbContext.CustomFields.RemoveRange(customFields);
        App.DbContext.SchemaDefinitions.Remove(definition);

        await App.DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Updates the display order of schema definitions.
    /// </summary>
    public async Task UpdateDisplayOrderAsync(IEnumerable<(int Id, int Order)> orderUpdates)
    {
        foreach (var (id, order) in orderUpdates)
        {
            var definition = await App.DbContext.SchemaDefinitions.FindAsync(id);
            if (definition != null)
            {
                definition.DisplayOrder = order;
            }
        }

        await App.DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Gets custom field value for an entity.
    /// </summary>
    public async Task<string?> GetCustomFieldValueAsync(string entityType, int entityId, string fieldName)
    {
        var field = await App.DbContext.CustomFields
            .FirstOrDefaultAsync(cf => cf.EntityType == entityType && 
                                       cf.EntityId == entityId && 
                                       cf.Key == fieldName);
        return field?.Value;
    }

    /// <summary>
    /// Gets all custom field values for an entity.
    /// </summary>
    public async Task<Dictionary<string, string?>> GetCustomFieldValuesAsync(string entityType, int entityId)
    {
        var fields = await App.DbContext.CustomFields
            .Where(cf => cf.EntityType == entityType && cf.EntityId == entityId)
            .ToListAsync();

        return fields.ToDictionary(f => f.Key, f => f.Value);
    }

    /// <summary>
    /// Sets a custom field value for an entity.
    /// </summary>
    public async Task SetCustomFieldValueAsync(string entityType, int entityId, string fieldName, string? value, CustomFieldType fieldType)
    {
        var existing = await App.DbContext.CustomFields
            .FirstOrDefaultAsync(cf => cf.EntityType == entityType && 
                                       cf.EntityId == entityId && 
                                       cf.Key == fieldName);

        if (existing != null)
        {
            existing.Value = value;
            existing.FieldType = fieldType;
        }
        else
        {
            App.DbContext.CustomFields.Add(new CustomField
            {
                EntityType = entityType,
                EntityId = entityId,
                Key = fieldName,
                Value = value,
                FieldType = fieldType,
                CreatedAt = DateTime.UtcNow
            });
        }

        await App.DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Gets the count of custom fields for each entity type.
    /// </summary>
    public async Task<Dictionary<string, int>> GetFieldCountsByEntityTypeAsync()
    {
        return await App.DbContext.SchemaDefinitions
            .Where(s => s.IsActive)
            .GroupBy(s => s.EntityType)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Exports schema definitions to JSON.
    /// </summary>
    public async Task<string> ExportSchemaDefinitionsAsync()
    {
        var definitions = await App.DbContext.SchemaDefinitions
            .Where(s => s.IsActive)
            .OrderBy(s => s.EntityType)
            .ThenBy(s => s.DisplayOrder)
            .ToListAsync();

        return System.Text.Json.JsonSerializer.Serialize(definitions, JsonOptions);
    }

    /// <summary>
    /// Imports schema definitions from JSON.
    /// </summary>
    public async Task<int> ImportSchemaDefinitionsAsync(string json, bool overwriteExisting = false)
    {
        var definitions = System.Text.Json.JsonSerializer.Deserialize<List<SchemaDefinition>>(json);
        if (definitions == null || definitions.Count == 0)
        {
            return 0;
        }

        int imported = 0;

        foreach (var definition in definitions)
        {
            var existing = await App.DbContext.SchemaDefinitions
                .FirstOrDefaultAsync(s => s.EntityType == definition.EntityType && 
                                         s.FieldName == definition.FieldName);

            if (existing != null)
            {
                if (overwriteExisting)
                {
                    existing.DisplayLabel = definition.DisplayLabel;
                    existing.FieldType = definition.FieldType;
                    existing.EnumOptions = definition.EnumOptions;
                    existing.DefaultValue = definition.DefaultValue;
                    existing.IsRequired = definition.IsRequired;
                    existing.Placeholder = definition.Placeholder;
                    existing.Description = definition.Description;
                    existing.MinValue = definition.MinValue;
                    existing.MaxValue = definition.MaxValue;
                    existing.MaxLength = definition.MaxLength;
                    existing.DisplayOrder = definition.DisplayOrder;
                    existing.IsActive = true;
                    existing.ModifiedAt = DateTime.UtcNow;
                    imported++;
                }
            }
            else
            {
                definition.Id = 0;
                definition.CreatedAt = DateTime.UtcNow;
                definition.ModifiedAt = null;
                App.DbContext.SchemaDefinitions.Add(definition);
                imported++;
            }
        }

        await App.DbContext.SaveChangesAsync();
        return imported;
    }
}
