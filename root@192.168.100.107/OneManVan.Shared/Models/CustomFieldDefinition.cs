using System.ComponentModel.DataAnnotations;

namespace OneManVan.Shared.Models;

/// <summary>
/// Enhanced custom field definition for dynamic schema management.
/// Supports various field types including dropdowns with choices.
/// </summary>
public class CustomFieldDefinition
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty; // Customer, Asset, Job, etc.

    [Required]
    [MaxLength(100)]
    public string FieldName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string FieldType { get; set; } = "Text"; // Text, TextArea, Number, Decimal, Date, DateTime, Checkbox, Dropdown, MultiSelect, Radio, Email, Phone, Url, Currency, Lookup

    public bool IsRequired { get; set; } = false;

    public bool IsReadOnly { get; set; } = false;

    /// <summary>
    /// System fields cannot be deleted (e.g., Name, Serial).
    /// </summary>
    public bool IsSystemField { get; set; } = false;

    [MaxLength(500)]
    public string? DefaultValue { get; set; }

    [MaxLength(200)]
    public string? Placeholder { get; set; }

    [MaxLength(500)]
    public string? HelpText { get; set; }

    [MaxLength(500)]
    public string? ValidationRegex { get; set; }

    public decimal? MinValue { get; set; }

    public decimal? MaxValue { get; set; }

    public int? MinLength { get; set; }

    public int? MaxLength { get; set; }

    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Group name for organizing fields in sections.
    /// </summary>
    [MaxLength(100)]
    public string? GroupName { get; set; }

    public bool IsVisible { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<CustomFieldChoice> Choices { get; set; } = [];
}

