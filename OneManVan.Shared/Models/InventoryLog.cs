using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneManVan.Shared.Models;

/// <summary>
/// Type of inventory change.
/// </summary>
public enum InventoryChangeType
{
    Initial = 0,
    Restock = 1,
    Adjustment = 2,
    UsedOnJob = 3,
    UsedOnEstimate = 4,
    Returned = 5,
    Damaged = 6,
    Expired = 7
}

/// <summary>
/// Log entry for inventory quantity changes.
/// </summary>
public class InventoryLog
{
    public int Id { get; set; }

    [Required]
    public int InventoryItemId { get; set; }

    public InventoryChangeType ChangeType { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal QuantityChange { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal QuantityBefore { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal QuantityAfter { get; set; }

    public int? EstimateId { get; set; }

    public int? JobId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public InventoryItem InventoryItem { get; set; } = null!;

    public string ChangeTypeDisplay => ChangeType switch
    {
        InventoryChangeType.Initial => "Initial Stock",
        InventoryChangeType.Restock => "Restocked",
        InventoryChangeType.Adjustment => "Adjustment",
        InventoryChangeType.UsedOnJob => "Used on Job",
        InventoryChangeType.UsedOnEstimate => "Reserved for Estimate",
        InventoryChangeType.Returned => "Returned",
        InventoryChangeType.Damaged => "Damaged/Lost",
        InventoryChangeType.Expired => "Expired",
        _ => "Unknown"
    };

    public string ChangeDisplay => QuantityChange >= 0 
        ? $"+{QuantityChange:N2}" 
        : QuantityChange.ToString("N2");
}
