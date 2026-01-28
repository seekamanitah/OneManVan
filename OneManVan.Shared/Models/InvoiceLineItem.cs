using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneManVan.Shared.Models;

/// <summary>
/// Individual line item on an invoice.
/// Can be created from custom entry, inventory, or products.
/// </summary>
public class InvoiceLineItem
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    /// <summary>
    /// Source of this line item: "Custom", "Inventory", "Product", "Labor"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Source { get; set; } = "Custom";

    /// <summary>
    /// If from Inventory or Product, the ID of that item
    /// </summary>
    public int? SourceId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Quantity { get; set; } = 1;

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Total => Quantity * UnitPrice;

    /// <summary>
    /// Optional: Serial number if this is a serialized product
    /// </summary>
    [MaxLength(100)]
    public string? SerialNumber { get; set; }

    /// <summary>
    /// Optional: If an asset was created from this line item
    /// </summary>
    public int? CreatedAssetId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Invoice Invoice { get; set; } = null!;
    public Asset? CreatedAsset { get; set; }
}

/// <summary>
/// Constants for line item sources
/// </summary>
public static class LineItemSource
{
    public const string Custom = "Custom";
    public const string Inventory = "Inventory";
    public const string Product = "Product";
    public const string Labor = "Labor";
}
