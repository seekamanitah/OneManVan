using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Individual line item in a material list.
/// </summary>
public class MaterialListItem
{
    public int Id { get; set; }

    /// <summary>
    /// Parent material list.
    /// </summary>
    [Required]
    public int MaterialListId { get; set; }

    /// <summary>
    /// System/zone this item belongs to (null if not using multi-system).
    /// </summary>
    public int? MaterialListSystemId { get; set; }

    // === Item Identification ===

    /// <summary>
    /// Main category (Ductwork, Equipment, Electrical, etc.).
    /// </summary>
    public MaterialCategory Category { get; set; } = MaterialCategory.Ductwork;

    /// <summary>
    /// Subcategory within the main category (e.g., FlexDuct, TakeOffs).
    /// </summary>
    [MaxLength(50)]
    public string? SubCategory { get; set; }

    /// <summary>
    /// Item name/description.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// Size specification (e.g., "6 inch", "4x10x6", "14 gauge").
    /// </summary>
    [MaxLength(50)]
    public string? Size { get; set; }

    /// <summary>
    /// Additional description or notes.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    // === Quantity ===

    /// <summary>
    /// Quantity needed.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal Quantity { get; set; } = 0;

    /// <summary>
    /// Unit of measure (each, box, ft, roll, etc.).
    /// </summary>
    [MaxLength(20)]
    public string Unit { get; set; } = "each";

    // === Pricing ===

    /// <summary>
    /// Cost per unit (from inventory or manual entry).
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitCost { get; set; } = 0;

    /// <summary>
    /// Whether the cost was manually overridden.
    /// </summary>
    public bool IsCostOverridden { get; set; } = false;

    /// <summary>
    /// Original cost from inventory (before override).
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? OriginalCost { get; set; }

    // === Inventory Link ===

    /// <summary>
    /// Link to inventory item (optional).
    /// </summary>
    public int? InventoryItemId { get; set; }

    /// <summary>
    /// Whether this item was sourced from inventory.
    /// </summary>
    public bool IsFromInventory { get; set; } = false;

    // === Custom Item Flag ===

    /// <summary>
    /// Whether this is a custom/one-off item added by user.
    /// </summary>
    public bool IsCustomItem { get; set; } = false;

    // === Display Order ===

    /// <summary>
    /// Sort order within category/subcategory.
    /// </summary>
    public int SortOrder { get; set; } = 0;

    // === Notes ===

    [MaxLength(500)]
    public string? Notes { get; set; }

    // === Navigation Properties ===

    public MaterialList MaterialList { get; set; } = null!;

    public MaterialListSystem? MaterialListSystem { get; set; }

    public InventoryItem? InventoryItem { get; set; }

    // === Computed Properties ===

    [NotMapped]
    public decimal TotalCost => Quantity * UnitCost;

    [NotMapped]
    public string CategoryDisplay => Category switch
    {
        MaterialCategory.Ductwork => "Ductwork",
        MaterialCategory.Equipment => "Equipment",
        MaterialCategory.Electrical => "Electrical",
        MaterialCategory.Refrigerant => "Refrigerant/Copper",
        MaterialCategory.Drain => "Drain/Plumbing",
        MaterialCategory.Miscellaneous => "Misc Materials",
        MaterialCategory.GrillsRegisters => "Grills/Registers",
        MaterialCategory.Accessories => "Accessories",
        MaterialCategory.Disposal => "Disposal/Removal",
        MaterialCategory.Permits => "Permits/Inspections",
        MaterialCategory.Other => "Other",
        _ => Category.ToString()
    };

    [NotMapped]
    public string DisplayName => !string.IsNullOrEmpty(Size) 
        ? $"{ItemName} ({Size})" 
        : ItemName;

    [NotMapped]
    public string QuantityDisplay => Unit.ToLower() switch
    {
        "each" => Quantity == 1 ? "1" : $"{Quantity:N0}",
        "box" or "boxes" => Quantity == 1 ? "1 box" : $"{Quantity:N0} boxes",
        "ft" or "feet" => $"{Quantity:N0} ft",
        "roll" or "rolls" => Quantity == 1 ? "1 roll" : $"{Quantity:N0} rolls",
        _ => $"{Quantity:N2} {Unit}"
    };
}
