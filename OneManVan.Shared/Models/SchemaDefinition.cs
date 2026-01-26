using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Defines a custom field schema that can be added to entities.
/// </summary>
public class SchemaDefinition
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FieldName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DisplayLabel { get; set; } = string.Empty;

    public CustomFieldType FieldType { get; set; } = CustomFieldType.Text;

    [MaxLength(500)]
    public string? EnumOptions { get; set; }

    [MaxLength(500)]
    public string? DefaultValue { get; set; }

    public bool IsRequired { get; set; } = false;

    [MaxLength(200)]
    public string? Placeholder { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MinValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaxValue { get; set; }

    public int? MaxLength { get; set; }

    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public bool IsFromPreset { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ModifiedAt { get; set; }

    [NotMapped]
    public List<string> EnumOptionsList => 
        string.IsNullOrWhiteSpace(EnumOptions) 
            ? [] 
            : EnumOptions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    [NotMapped]
    public string FieldTypeDisplay => FieldType switch
    {
        CustomFieldType.Text => "Text",
        CustomFieldType.Number => "Number",
        CustomFieldType.Decimal => "Decimal",
        CustomFieldType.Date => "Date",
        CustomFieldType.Boolean => "Yes/No",
        CustomFieldType.Enum => "Dropdown",
        _ => "Unknown"
    };
}

/// <summary>
/// Entity types that support custom fields.
/// </summary>
public static class SchemaEntityTypes
{
    public const string Customer = "Customer";
    public const string Site = "Site";
    public const string Asset = "Asset";
    public const string Job = "Job";
    public const string Estimate = "Estimate";
    public const string Invoice = "Invoice";

    public static readonly string[] All = [Customer, Site, Asset, Job, Estimate, Invoice];
}
