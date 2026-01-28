using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Estimate/proposal for a customer job.
/// </summary>
public class Estimate
{
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; }

    public int? SiteId { get; set; }

    public int? AssetId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public EstimateStatus Status { get; set; } = EstimateStatus.Draft;

    [Column(TypeName = "decimal(10,2)")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal TaxRate { get; set; } = 0;

    [Column(TypeName = "decimal(10,2)")]
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// When true, the price already includes tax (no additional tax calculation needed).
    /// </summary>
    public bool TaxIncluded { get; set; } = false;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(2000)]
    public string? Terms { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SentAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? AcceptedAt { get; set; }

    // Navigation properties
    public Customer Customer { get; set; } = null!;

    public Site? Site { get; set; }

    public Asset? Asset { get; set; }

    public ICollection<EstimateLine> Lines { get; set; } = [];

    public void RecalculateTotals()
    {
        SubTotal = Lines.Sum(l => l.Total);
        if (TaxIncluded)
        {
            // Tax is already included in prices, no additional tax
            TaxAmount = 0;
            Total = SubTotal;
        }
        else
        {
            TaxAmount = SubTotal * (TaxRate / 100);
            Total = SubTotal + TaxAmount;
        }
    }

    public string StatusDisplay => Status switch
    {
        EstimateStatus.Draft => "Draft",
        EstimateStatus.Sent => "Sent",
        EstimateStatus.Accepted => "Accepted",
        EstimateStatus.Declined => "Declined",
        EstimateStatus.Expired => "Expired",
        EstimateStatus.Converted => "Converted",
        _ => "Unknown"
    };

    public bool CanEdit => Status == EstimateStatus.Draft;

    public bool IsValid => ExpiresAt == null || ExpiresAt > DateTime.UtcNow;
}
