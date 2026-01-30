using System.ComponentModel.DataAnnotations;

namespace OneManVan.Shared.Models;

/// <summary>
/// Junction table for multi-ownership of assets.
/// Allows assets to be owned by multiple customers and/or companies.
/// </summary>
public class AssetOwner
{
    public int Id { get; set; }

    [Required]
    public int AssetId { get; set; }

    [Required]
    [MaxLength(20)]
    public string OwnerType { get; set; } = string.Empty; // "Customer" or "Company"

    [Required]
    public int OwnerId { get; set; } // Polymorphic reference

    [MaxLength(50)]
    public string OwnershipType { get; set; } = "Primary"; // Primary, Shared, Leased, Managed

    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    public DateTime? EndDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Asset Asset { get; set; } = null!;
}
