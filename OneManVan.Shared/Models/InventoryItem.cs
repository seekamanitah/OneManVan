using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Inventory item for parts, materials, and supplies.
/// </summary>
public class InventoryItem
{
    public int Id { get; set; }

    /// <summary>
    /// Auto-generated inventory number (e.g., INV-0001).
    /// </summary>
    [MaxLength(20)]
    public string? InventoryNumber { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Sku { get; set; }

    [MaxLength(50)]
    public string? PartNumber { get; set; }

    public InventoryCategory Category { get; set; } = InventoryCategory.General;

    [Column(TypeName = "decimal(10,2)")]
    public decimal QuantityOnHand { get; set; }

    [MaxLength(20)]
    public string Unit { get; set; } = "ea";

    [Column(TypeName = "decimal(10,2)")]
    public decimal Cost { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal ReorderPoint { get; set; } = 0;

    // HVAC-specific fields
    public int? BtuMinCompatibility { get; set; }

    public int? BtuMaxCompatibility { get; set; }

    public FuelType? FuelTypeCompatibility { get; set; }

    public UnitConfig? UnitConfigCompatibility { get; set; }

    [MaxLength(100)]
    public string? FilterSize { get; set; }

    [MaxLength(100)]
    public string? RefrigerantType { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    [MaxLength(100)]
    public string? Supplier { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastRestockedAt { get; set; }

    // Navigation properties
    public ICollection<InventoryLog> Logs { get; set; } = [];

    public ICollection<EstimateLine> EstimateLines { get; set; } = [];

    public bool IsLowStock => QuantityOnHand <= ReorderPoint;

    public bool IsOutOfStock => QuantityOnHand <= 0;

    public decimal ProfitMargin => Cost > 0 ? ((Price - Cost) / Cost) * 100 : 0;

    public bool IsCompatibleWithBtu(int? btu)
    {
        if (btu == null) return true;
        if (BtuMinCompatibility == null && BtuMaxCompatibility == null) return true;
        
        var min = BtuMinCompatibility ?? 0;
        var max = BtuMaxCompatibility ?? int.MaxValue;
        return btu >= min && btu <= max;
    }

    public bool IsCompatibleWithFuelType(FuelType? fuelType)
    {
        if (FuelTypeCompatibility == null) return true;
        return fuelType == FuelTypeCompatibility;
    }
}
