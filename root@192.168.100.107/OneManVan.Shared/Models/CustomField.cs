using System.ComponentModel.DataAnnotations;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Custom field for no-code schema extensions.
/// </summary>
public class CustomField
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    public int EntityId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    public CustomFieldType FieldType { get; set; } = CustomFieldType.Text;

    [MaxLength(1000)]
    public string? Value { get; set; }

    [MaxLength(500)]
    public string? EnumOptions { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public int? AssetId { get; set; }
    public Asset? Asset { get; set; }
}
