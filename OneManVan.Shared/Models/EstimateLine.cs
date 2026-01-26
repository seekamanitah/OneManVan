using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Line item in an estimate.
/// </summary>
public class EstimateLine
{
    public int Id { get; set; }

    [Required]
    public int EstimateId { get; set; }

    public int? InventoryItemId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    public LineItemType Type { get; set; } = LineItemType.Labor;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Quantity { get; set; } = 1;

    [MaxLength(20)]
    public string Unit { get; set; } = "ea";

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    public int SortOrder { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    // Navigation properties
    public Estimate Estimate { get; set; } = null!;

    public InventoryItem? InventoryItem { get; set; }

    public void RecalculateTotal()
    {
        Total = Type == LineItemType.Discount 
            ? -Math.Abs(Quantity * UnitPrice) 
            : Quantity * UnitPrice;
    }

    public string TypeDisplay => Type switch
    {
        LineItemType.Labor => "Labor",
        LineItemType.Part => "Part",
        LineItemType.Material => "Material",
        LineItemType.Equipment => "Equipment",
        LineItemType.Service => "Service",
        LineItemType.Discount => "Discount",
        LineItemType.Fee => "Fee",
        _ => "Other"
    };
}
