using System.ComponentModel.DataAnnotations;

namespace OneManVan.Shared.Models;

/// <summary>
/// Enhanced custom field choices for dropdown, multi-select, and radio fields.
/// </summary>
public class CustomFieldChoice
{
    public int Id { get; set; }

    [Required]
    public int FieldDefinitionId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Value { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string DisplayText { get; set; } = string.Empty;

    [MaxLength(7)]
    public string? Color { get; set; } // Hex color for visual distinction

    [MaxLength(50)]
    public string? Icon { get; set; }

    public int DisplayOrder { get; set; } = 0;

    public bool IsDefault { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public CustomFieldDefinition FieldDefinition { get; set; } = null!;
}
