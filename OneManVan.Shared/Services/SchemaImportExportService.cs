using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Shared.Services;

/// <summary>
/// Service for importing and exporting schema definitions.
/// </summary>
public class SchemaImportExportService
{
    private readonly OneManVanDbContext _dbContext;

    public SchemaImportExportService(OneManVanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Exports schema for a specific entity type to JSON.
    /// </summary>
    public async Task<string> ExportSchemaAsync(string entityType)
    {
        var fields = await _dbContext.CustomFieldDefinitions
            .Include(f => f.Choices)
            .Where(f => f.EntityType == entityType)
            .OrderBy(f => f.DisplayOrder)
            .ToListAsync();

        var schema = new SchemaExport
        {
            SchemaVersion = "1.0",
            EntityType = entityType,
            ExportDate = DateTime.UtcNow,
            Fields = fields.Select(f => new FieldExport
            {
                FieldName = f.FieldName,
                DisplayName = f.DisplayName,
                FieldType = f.FieldType,
                IsRequired = f.IsRequired,
                IsReadOnly = f.IsReadOnly,
                IsSystemField = f.IsSystemField,
                DefaultValue = f.DefaultValue,
                Placeholder = f.Placeholder,
                HelpText = f.HelpText,
                ValidationRegex = f.ValidationRegex,
                MinValue = f.MinValue,
                MaxValue = f.MaxValue,
                MinLength = f.MinLength,
                MaxLength = f.MaxLength,
                DisplayOrder = f.DisplayOrder,
                GroupName = f.GroupName,
                IsVisible = f.IsVisible,
                Choices = f.Choices.Select(c => new ChoiceExport
                {
                    Value = c.Value,
                    DisplayText = c.DisplayText,
                    Color = c.Color,
                    Icon = c.Icon,
                    DisplayOrder = c.DisplayOrder,
                    IsDefault = c.IsDefault,
                    IsActive = c.IsActive
                }).ToList()
            }).ToList()
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(schema, options);
    }

    /// <summary>
    /// Imports schema from JSON.
    /// </summary>
    public async Task<SchemaImportResult> ImportSchemaAsync(string json, bool replaceExisting = false)
    {
        var result = new SchemaImportResult();

        try
        {
            var schema = JsonSerializer.Deserialize<SchemaExport>(json);
            if (schema == null)
            {
                result.Success = false;
                result.ErrorMessage = "Invalid schema format";
                return result;
            }

            if (replaceExisting)
            {
                // Remove existing fields for this entity type
                var existingFields = await _dbContext.CustomFieldDefinitions
                    .Include(f => f.Choices)
                    .Where(f => f.EntityType == schema.EntityType && !f.IsSystemField)
                    .ToListAsync();

                _dbContext.CustomFieldDefinitions.RemoveRange(existingFields);
            }

            // Import fields
            foreach (var fieldExport in schema.Fields)
            {
                // Check if field already exists
                var existingField = await _dbContext.CustomFieldDefinitions
                    .FirstOrDefaultAsync(f => f.EntityType == schema.EntityType && f.FieldName == fieldExport.FieldName);

                if (existingField != null)
                {
                    result.SkippedFields.Add(fieldExport.FieldName);
                    continue;
                }

                var field = new CustomFieldDefinition
                {
                    EntityType = schema.EntityType,
                    FieldName = fieldExport.FieldName,
                    DisplayName = fieldExport.DisplayName,
                    FieldType = fieldExport.FieldType,
                    IsRequired = fieldExport.IsRequired,
                    IsReadOnly = fieldExport.IsReadOnly,
                    IsSystemField = fieldExport.IsSystemField,
                    DefaultValue = fieldExport.DefaultValue,
                    Placeholder = fieldExport.Placeholder,
                    HelpText = fieldExport.HelpText,
                    ValidationRegex = fieldExport.ValidationRegex,
                    MinValue = fieldExport.MinValue,
                    MaxValue = fieldExport.MaxValue,
                    MinLength = fieldExport.MinLength,
                    MaxLength = fieldExport.MaxLength,
                    DisplayOrder = fieldExport.DisplayOrder,
                    GroupName = fieldExport.GroupName,
                    IsVisible = fieldExport.IsVisible
                };

                _dbContext.CustomFieldDefinitions.Add(field);
                await _dbContext.SaveChangesAsync(); // Save to get the field ID

                // Import choices
                foreach (var choiceExport in fieldExport.Choices)
                {
                    var choice = new CustomFieldChoice
                    {
                        FieldDefinitionId = field.Id,
                        Value = choiceExport.Value,
                        DisplayText = choiceExport.DisplayText,
                        Color = choiceExport.Color,
                        Icon = choiceExport.Icon,
                        DisplayOrder = choiceExport.DisplayOrder,
                        IsDefault = choiceExport.IsDefault,
                        IsActive = choiceExport.IsActive
                    };

                    _dbContext.CustomFieldChoices.Add(choice);
                }

                result.ImportedFields.Add(fieldExport.FieldName);
            }

            await _dbContext.SaveChangesAsync();
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Gets available schema templates.
    /// </summary>
    public List<SchemaTemplate> GetTemplates()
    {
        return new List<SchemaTemplate>
        {
            new()
            {
                Name = "HVAC Asset Fields",
                Description = "Standard HVAC equipment fields with brand, model, serial, etc.",
                EntityType = "Asset",
                FieldCount = 12
            },
            new()
            {
                Name = "Customer Contact Fields",
                Description = "Comprehensive customer contact information fields",
                EntityType = "Customer",
                FieldCount = 8
            },
            new()
            {
                Name = "Property Management",
                Description = "Fields for managing properties and tenants",
                EntityType = "Customer",
                FieldCount = 10
            }
        };
    }
}

/// <summary>
/// Schema export format.
/// </summary>
public class SchemaExport
{
    public string SchemaVersion { get; set; } = "1.0";
    public string EntityType { get; set; } = string.Empty;
    public DateTime ExportDate { get; set; }
    public string? ExportedBy { get; set; }
    public string? Notes { get; set; }
    public List<FieldExport> Fields { get; set; } = [];
}

public class FieldExport
{
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsSystemField { get; set; }
    public string? DefaultValue { get; set; }
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    public string? ValidationRegex { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public int DisplayOrder { get; set; }
    public string? GroupName { get; set; }
    public bool IsVisible { get; set; } = true;
    public List<ChoiceExport> Choices { get; set; } = [];
}

public class ChoiceExport
{
    public string Value { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SchemaTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int FieldCount { get; set; }
}

public class SchemaImportResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> ImportedFields { get; set; } = [];
    public List<string> SkippedFields { get; set; } = [];
}
