using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Tracks warranty claims and repairs for assets.
/// Links equipment failures/issues to warranty coverage and manufacturer response.
/// </summary>
public class WarrantyClaim
{
    public int Id { get; set; }

    // === Asset Link ===

    /// <summary>
    /// The asset this claim is for.
    /// </summary>
    [Required]
    public int AssetId { get; set; }

    /// <summary>
    /// Navigation property to the asset.
    /// </summary>
    public Asset? Asset { get; set; }

    // === Claim Identification ===

    /// <summary>
    /// Unique claim number (e.g., CLM-0001).
    /// </summary>
    [MaxLength(50)]
    public string? ClaimNumber { get; set; }

    /// <summary>
    /// Date claim was filed.
    /// </summary>
    [Required]
    public DateTime ClaimDate { get; set; } = DateTime.Today;

    /// <summary>
    /// Date claim was resolved (approved/denied/completed).
    /// </summary>
    public DateTime? ResolvedDate { get; set; }

    // === Issue Details ===

    /// <summary>
    /// Description of the issue/failure.
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string IssueDescription { get; set; } = string.Empty;

    /// <summary>
    /// Resolution or outcome of the claim.
    /// </summary>
    [MaxLength(2000)]
    public string? Resolution { get; set; }

    /// <summary>
    /// Parts that were replaced during repair.
    /// </summary>
    [MaxLength(1000)]
    public string? PartsReplaced { get; set; }

    // === Warranty Coverage ===

    /// <summary>
    /// Is this a warranty claim vs general repair record?
    /// When false, this is just repair history tracking (no warranty involvement).
    /// </summary>
    public bool IsWarrantyClaim { get; set; } = true;

    /// <summary>
    /// Is this claim covered by warranty?
    /// Only applicable when IsWarrantyClaim is true.
    /// </summary>
    public bool IsCoveredByWarranty { get; set; } = true;

    /// <summary>
    /// Reason if not covered (expired, misuse, etc.).
    /// </summary>
    [MaxLength(500)]
    public string? NotCoveredReason { get; set; }

    // === Financial ===

    /// <summary>
    /// Total cost of repair/parts.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal RepairCost { get; set; }

    /// <summary>
    /// Amount charged to customer (if not fully covered).
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal CustomerCharge { get; set; }

    // === Status ===

    /// <summary>
    /// Current status of the claim.
    /// </summary>
    [Required]
    public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

    // === Job Link (Optional) ===

    /// <summary>
    /// Link to job if repair was done by your company.
    /// </summary>
    public int? JobId { get; set; }

    /// <summary>
    /// Navigation property to the job.
    /// </summary>
    public Job? Job { get; set; }

    // === Manufacturer Response ===

    /// <summary>
    /// Manufacturer's response to the claim.
    /// </summary>
    [MaxLength(2000)]
    public string? ManufacturerResponse { get; set; }

    /// <summary>
    /// Date manufacturer responded.
    /// </summary>
    public DateTime? ManufacturerResponseDate { get; set; }

    /// <summary>
    /// Manufacturer claim reference number.
    /// </summary>
    [MaxLength(100)]
    public string? ManufacturerClaimNumber { get; set; }

    // === Additional Info ===

    /// <summary>
    /// Technician notes about the issue/repair.
    /// </summary>
    [MaxLength(2000)]
    public string? TechnicianNotes { get; set; }

    /// <summary>
    /// Who filed the claim.
    /// </summary>
    [MaxLength(100)]
    public string? FiledBy { get; set; }

    // === Timestamps ===

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
