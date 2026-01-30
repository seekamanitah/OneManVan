using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneManVan.Shared.Models;

/// <summary>
/// Service history entry for tracking work performed on an asset.
/// </summary>
public class ServiceHistory
{
    public int Id { get; set; }

    [Required]
    public int AssetId { get; set; }

    public int? JobId { get; set; }

    public DateTime ServiceDate { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(100)]
    public string ServiceType { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(2000)]
    public string? TechnicianNotes { get; set; }

    [MaxLength(1000)]
    public string? PartsReplaced { get; set; }

    /// <summary>
    /// JSON object with readings (pressures, temps, etc.).
    /// </summary>
    [MaxLength(4000)]
    public string? Readings { get; set; }

    [MaxLength(100)]
    public string? PerformedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Asset Asset { get; set; } = null!;
    public Job? Job { get; set; }

    [NotMapped]
    public string ServiceDateDisplay => ServiceDate.ToString("MMM d, yyyy");
}

/// <summary>
/// Parts used on a job, linked to inventory.
/// </summary>
public class JobPart
{
    public int Id { get; set; }

    [Required]
    public int JobId { get; set; }

    public int? InventoryItemId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? PartNumber { get; set; }

    public int Quantity { get; set; } = 1;

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitCost { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    public bool IsWarrantyPart { get; set; } = false;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Job Job { get; set; } = null!;
    public InventoryItem? InventoryItem { get; set; }

    // Computed
    [NotMapped]
    public decimal Profit => (UnitPrice - UnitCost) * Quantity;
}
