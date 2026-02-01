using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Material list for HVAC job bidding and material tracking.
/// Links to customer/site for historical reference.
/// </summary>
public class MaterialList
{
    public int Id { get; set; }

    /// <summary>
    /// Auto-generated list number (e.g., ML-2024-0001).
    /// </summary>
    [MaxLength(20)]
    public string? ListNumber { get; set; }

    /// <summary>
    /// Descriptive title (e.g., "3-Ton Split System Install - Smith Residence").
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    // === Relationships ===

    /// <summary>
    /// Customer this material list is for (required).
    /// </summary>
    [Required]
    public int CustomerId { get; set; }

    /// <summary>
    /// Site/location for this job (optional but recommended).
    /// </summary>
    public int? SiteId { get; set; }

    /// <summary>
    /// Linked job if created from or converted to a job.
    /// </summary>
    public int? JobId { get; set; }

    /// <summary>
    /// Linked estimate if converted to estimate.
    /// </summary>
    public int? EstimateId { get; set; }

    // === Property Information ===

    /// <summary>
    /// Square footage of the space being serviced.
    /// </summary>
    public int? SquareFootage { get; set; }

    /// <summary>
    /// Number of zones in the system.
    /// </summary>
    public int Zones { get; set; } = 1;

    /// <summary>
    /// Number of stories in the building.
    /// </summary>
    public int Stories { get; set; } = 1;

    // === Status ===

    public MaterialListStatus Status { get; set; } = MaterialListStatus.Draft;

    /// <summary>
    /// When the list was finalized.
    /// </summary>
    public DateTime? FinalizedAt { get; set; }

    /// <summary>
    /// Who finalized the list.
    /// </summary>
    [MaxLength(100)]
    public string? FinalizedBy { get; set; }

    // === Cost Totals (calculated) ===

    /// <summary>
    /// Total material cost (sum of all items).
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalMaterialCost { get; set; }

    /// <summary>
    /// Markup percentage for pricing.
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal MarkupPercent { get; set; } = 0;

    /// <summary>
    /// Total with markup applied.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalWithMarkup { get; set; }

    // === Labor Calculation ===

    /// <summary>
    /// Estimated labor hours.
    /// </summary>
    [Column(TypeName = "decimal(6,2)")]
    public decimal? LaborHours { get; set; }

    /// <summary>
    /// Hourly labor rate.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? LaborHourlyRate { get; set; }

    /// <summary>
    /// Fixed labor price (overrides hourly calculation if set).
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? LaborFixedPrice { get; set; }

    /// <summary>
    /// Calculated or fixed labor total.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal LaborTotal { get; set; }

    /// <summary>
    /// Contingency percentage (e.g., 10-15%).
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal ContingencyPercent { get; set; } = 0;

    /// <summary>
    /// Tax percentage.
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal TaxPercent { get; set; } = 0;

    // === Disposal & Permits ===

    /// <summary>
    /// Old unit haul-away fee.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal DisposalFee { get; set; } = 0;

    /// <summary>
    /// Refrigerant reclaim fee.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal RefrigerantReclaimFee { get; set; } = 0;

    /// <summary>
    /// Permit cost.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal PermitCost { get; set; } = 0;

    /// <summary>
    /// Inspection fees.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal InspectionFees { get; set; } = 0;

    /// <summary>
    /// Permit type description.
    /// </summary>
    [MaxLength(100)]
    public string? PermitType { get; set; }

    /// <summary>
    /// Scheduled inspection date.
    /// </summary>
    public DateTime? InspectionDate { get; set; }

    // === Priority ===

    /// <summary>
    /// Job priority/urgency level.
    /// </summary>
    public JobPriority Priority { get; set; } = JobPriority.Normal;

    // === Financial Summary (Calculated) ===

    /// <summary>
    /// Accessories total (from items).
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal AccessoriesTotal { get; set; }

    /// <summary>
    /// Contingency amount (calculated from percent).
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal ContingencyAmount { get; set; }

    /// <summary>
    /// Tax amount (calculated).
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Grand total bid price.
    /// </summary>
    [Column(TypeName = "decimal(12,2)")]
    public decimal TotalBidPrice { get; set; }

    // === Notes ===

    /// <summary>
    /// General notes about the job/materials.
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Internal notes (not shown to customer).
    /// </summary>
    [MaxLength(2000)]
    public string? InternalNotes { get; set; }

    /// <summary>
    /// Labor notes.
    /// </summary>
    [MaxLength(1000)]
    public string? LaborNotes { get; set; }

    // === Metadata ===

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    [MaxLength(100)]
    public string? LastModifiedBy { get; set; }

    // === Navigation Properties ===

    public Customer Customer { get; set; } = null!;

    public Site? Site { get; set; }

    public Job? Job { get; set; }

    public Estimate? Estimate { get; set; }

    /// <summary>
    /// Systems/zones within this material list (collapsible sections).
    /// </summary>
    public ICollection<MaterialListSystem> Systems { get; set; } = [];

    /// <summary>
    /// All line items across all systems.
    /// </summary>
    public ICollection<MaterialListItem> Items { get; set; } = [];

    // === Computed Properties ===

    [NotMapped]
    public bool IsFinalized => Status == MaterialListStatus.Finalized;

    [NotMapped]
    public bool HasMultipleSystems => Systems.Count > 1;

    [NotMapped]
    public int TotalItemCount => Items.Count;

    [NotMapped]
    public string StatusDisplay => Status switch
    {
        MaterialListStatus.Draft => "Draft",
        MaterialListStatus.Finalized => "Finalized",
        _ => Status.ToString()
    };

    [NotMapped]
    public string PriorityDisplay => Priority switch
    {
        JobPriority.Emergency => "Emergency",
        JobPriority.Urgent => "Urgent",
        JobPriority.High => "High",
        JobPriority.Normal => "Normal",
        JobPriority.Low => "Low",
        _ => Priority.ToString()
    };

    [NotMapped]
    public decimal CalculatedLaborTotal => LaborFixedPrice ?? (LaborHours ?? 0) * (LaborHourlyRate ?? 0);

    [NotMapped]
    public decimal SubtotalBeforeTax => TotalMaterialCost + LaborTotal + AccessoriesTotal +
                                        DisposalFee + RefrigerantReclaimFee + PermitCost + 
                                        InspectionFees + ContingencyAmount;

    /// <summary>
    /// Recalculates all financial totals.
    /// </summary>
    public void RecalculateFinancials()
    {
        // Labor total
        LaborTotal = LaborFixedPrice ?? (LaborHours ?? 0) * (LaborHourlyRate ?? 0);

        // Material cost with markup
        TotalWithMarkup = TotalMaterialCost * (1 + MarkupPercent / 100);

        // Contingency on subtotal before tax
        var subtotal = TotalWithMarkup + LaborTotal + AccessoriesTotal + 
                       DisposalFee + RefrigerantReclaimFee + PermitCost + InspectionFees;
        ContingencyAmount = subtotal * (ContingencyPercent / 100);

        // Tax on everything
        var taxableAmount = subtotal + ContingencyAmount;
        TaxAmount = taxableAmount * (TaxPercent / 100);

        // Grand total
        TotalBidPrice = taxableAmount + TaxAmount;
    }
}
