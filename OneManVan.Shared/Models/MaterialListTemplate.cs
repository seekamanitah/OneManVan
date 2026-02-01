using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Reusable template for material lists.
/// Contains pre-defined items that can be applied to new material lists.
/// </summary>
public class MaterialListTemplate
{
    public int Id { get; set; }

    /// <summary>
    /// Template name (e.g., "Standard 2-Ton Residential Install").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this template is for.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Primary system type this template is designed for.
    /// </summary>
    public HvacSystemType SystemType { get; set; } = HvacSystemType.SplitSystem;

    /// <summary>
    /// Whether this is a built-in (system) template vs user-created.
    /// </summary>
    public bool IsBuiltIn { get; set; } = false;

    /// <summary>
    /// Whether this template is active and available for use.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Sort order for display in template picker.
    /// </summary>
    public int SortOrder { get; set; } = 0;

    // === Metadata ===

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    // === Navigation Properties ===

    /// <summary>
    /// Template items.
    /// </summary>
    public ICollection<MaterialListTemplateItem> Items { get; set; } = [];

    // === Computed Properties ===

    [NotMapped]
    public int ItemCount => Items.Count;

    [NotMapped]
    public string SystemTypeDisplay => SystemType switch
    {
        HvacSystemType.Spider => "Spider System",
        HvacSystemType.Trunk => "Trunk & Branch",
        HvacSystemType.HeatPump => "Heat Pump",
        HvacSystemType.GasFurnace => "Gas Furnace",
        HvacSystemType.SplitSystem => "Split System",
        HvacSystemType.PackageUnit => "Package Unit",
        HvacSystemType.MiniSplit => "Mini-Split",
        _ => "General"
    };
}

/// <summary>
/// Individual item within a material list template.
/// </summary>
public class MaterialListTemplateItem
{
    public int Id { get; set; }

    /// <summary>
    /// Parent template.
    /// </summary>
    [Required]
    public int MaterialListTemplateId { get; set; }

    // === Item Definition ===

    /// <summary>
    /// Category for this item.
    /// </summary>
    public MaterialCategory Category { get; set; } = MaterialCategory.Ductwork;

    /// <summary>
    /// Subcategory (e.g., "FlexDuct", "TakeOffs").
    /// </summary>
    [MaxLength(50)]
    public string? SubCategory { get; set; }

    /// <summary>
    /// Item name.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// Size specification.
    /// </summary>
    [MaxLength(50)]
    public string? Size { get; set; }

    /// <summary>
    /// Default quantity (0 = user must fill in).
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal DefaultQuantity { get; set; } = 0;

    /// <summary>
    /// Unit of measure.
    /// </summary>
    [MaxLength(20)]
    public string Unit { get; set; } = "each";

    /// <summary>
    /// Default cost per unit (0 = lookup from inventory).
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal DefaultUnitCost { get; set; } = 0;

    /// <summary>
    /// Link to inventory item for automatic pricing.
    /// </summary>
    public int? InventoryItemId { get; set; }

    /// <summary>
    /// Sort order within category.
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Whether this item is required (always included).
    /// </summary>
    public bool IsRequired { get; set; } = false;

    // === Navigation Properties ===

    public MaterialListTemplate Template { get; set; } = null!;

    public InventoryItem? InventoryItem { get; set; }

    // === Computed Properties ===

    [NotMapped]
    public string DisplayName => !string.IsNullOrEmpty(Size) 
        ? $"{ItemName} ({Size})" 
        : ItemName;
}
