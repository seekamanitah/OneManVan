using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Service Agreement (maintenance contract) for recurring maintenance services.
/// </summary>
public class ServiceAgreement
{
    public int Id { get; set; }

    /// <summary>
    /// Auto-generated agreement number (e.g., SA-2024-0001).
    /// </summary>
    [MaxLength(20)]
    public string? AgreementNumber { get; set; }

    [Required]
    public int CustomerId { get; set; }

    /// <summary>
    /// Optional site restriction (null = all customer sites).
    /// </summary>
    public int? SiteId { get; set; }

    // === Agreement Details ===

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public AgreementType Type { get; set; } = AgreementType.Annual;

    public AgreementStatus Status { get; set; } = AgreementStatus.Draft;

    // === Term ===

    public DateTime StartDate { get; set; } = DateTime.Today;

    public DateTime EndDate { get; set; } = DateTime.Today.AddYears(1);

    /// <summary>
    /// Whether agreement auto-renews at expiration.
    /// </summary>
    public bool AutoRenew { get; set; } = true;

    /// <summary>
    /// Days before expiration to send renewal reminder.
    /// </summary>
    public int RenewalReminderDays { get; set; } = 30;

    // === Pricing ===

    /// <summary>
    /// Total annual contract value.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal AnnualPrice { get; set; }

    /// <summary>
    /// Monthly payment amount (if billing monthly).
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal MonthlyPrice { get; set; }

    public BillingFrequency BillingFrequency { get; set; } = BillingFrequency.Annual;

    /// <summary>
    /// Discount percentage on repairs (e.g., 15% off parts/labor).
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal RepairDiscountPercent { get; set; } = 15m;

    /// <summary>
    /// Priority service (shorter response time).
    /// </summary>
    public bool PriorityService { get; set; } = true;

    /// <summary>
    /// Waive trip charge for service calls.
    /// </summary>
    public bool WaiveTripCharge { get; set; } = true;

    // === Included Services ===

    /// <summary>
    /// Number of included maintenance visits per year.
    /// </summary>
    public int IncludedVisitsPerYear { get; set; } = 2;

    /// <summary>
    /// Number of visits used this contract period.
    /// </summary>
    public int VisitsUsed { get; set; } = 0;

    /// <summary>
    /// Whether AC tune-up is included.
    /// </summary>
    public bool IncludesAcTuneUp { get; set; } = true;

    /// <summary>
    /// Whether heating tune-up is included.
    /// </summary>
    public bool IncludesHeatingTuneUp { get; set; } = true;

    /// <summary>
    /// Whether filter replacement is included.
    /// </summary>
    public bool IncludesFilterReplacement { get; set; } = false;

    /// <summary>
    /// Whether refrigerant top-off is included (up to X lbs).
    /// </summary>
    public bool IncludesRefrigerantTopOff { get; set; } = false;

    /// <summary>
    /// Max refrigerant lbs included per year if enabled.
    /// </summary>
    public int MaxRefrigerantLbsIncluded { get; set; } = 0;

    /// <summary>
    /// Whether parts are covered (limited coverage).
    /// </summary>
    public bool IncludesLimitedPartsCoverage { get; set; } = false;

    /// <summary>
    /// Max parts coverage amount per year.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal MaxPartsCoverageAmount { get; set; } = 0;

    // === Scheduling ===

    /// <summary>
    /// Preferred month for spring maintenance (1-12).
    /// </summary>
    public int? PreferredSpringMonth { get; set; } = 4; // April

    /// <summary>
    /// Preferred month for fall maintenance (1-12).
    /// </summary>
    public int? PreferredFallMonth { get; set; } = 10; // October

    /// <summary>
    /// Last date maintenance was scheduled.
    /// </summary>
    public DateTime? LastMaintenanceScheduled { get; set; }

    /// <summary>
    /// Next scheduled maintenance date.
    /// </summary>
    public DateTime? NextMaintenanceDue { get; set; }

    // === Equipment Coverage ===

    /// <summary>
    /// Comma-separated list of covered Asset IDs (null = all customer assets).
    /// </summary>
    [MaxLength(500)]
    public string? CoveredAssetIds { get; set; }

    /// <summary>
    /// Max age of equipment for coverage (years).
    /// </summary>
    public int? MaxEquipmentAgeYears { get; set; }

    // === Notes & Terms ===

    [MaxLength(2000)]
    public string? Terms { get; set; }

    [MaxLength(2000)]
    public string? InternalNotes { get; set; }

    // === Tracking ===

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ActivatedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    // === Navigation Properties ===

    public Customer Customer { get; set; } = null!;

    public Site? Site { get; set; }

    // === Computed Properties ===

    [NotMapped]
    public bool IsActive => Status == AgreementStatus.Active && 
                            DateTime.Today >= StartDate && 
                            DateTime.Today <= EndDate;

    [NotMapped]
    public bool IsExpiringSoon => IsActive && 
                                  EndDate <= DateTime.Today.AddDays(RenewalReminderDays);

    [NotMapped]
    public bool IsExpired => DateTime.Today > EndDate;

    [NotMapped]
    public int DaysUntilExpiration => (EndDate - DateTime.Today).Days;

    [NotMapped]
    public int VisitsRemaining => Math.Max(0, IncludedVisitsPerYear - VisitsUsed);

    [NotMapped]
    public string StatusDisplay => Status switch
    {
        AgreementStatus.Draft => "Draft",
        AgreementStatus.Pending => "Pending",
        AgreementStatus.Active => IsExpired ? "Expired" : (IsExpiringSoon ? "Expiring Soon" : "Active"),
        AgreementStatus.Expired => "Expired",
        AgreementStatus.Cancelled => "Cancelled",
        AgreementStatus.Suspended => "Suspended",
        _ => "Unknown"
    };

    [NotMapped]
    public string TypeDisplay => Type switch
    {
        AgreementType.Basic => "Basic",
        AgreementType.Standard => "Standard",
        AgreementType.Premium => "Premium",
        AgreementType.Annual => "Annual",
        AgreementType.SemiAnnual => "Semi-Annual",
        AgreementType.Quarterly => "Quarterly",
        AgreementType.Custom => "Custom",
        _ => "Unknown"
    };

    [NotMapped]
    public List<int> CoveredAssetIdList => string.IsNullOrWhiteSpace(CoveredAssetIds)
        ? []
        : CoveredAssetIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToList();

    /// <summary>
    /// Generates the next agreement number.
    /// </summary>
    public static string GenerateAgreementNumber(int lastNumber)
    {
        return $"SA-{DateTime.UtcNow:yyyy}-{(lastNumber + 1):D4}";
    }

    /// <summary>
    /// Calculates the next maintenance due date based on preferences.
    /// </summary>
    public DateTime? CalculateNextMaintenanceDue()
    {
        var today = DateTime.Today;
        
        // If both spring and fall are set, find the next one
        if (PreferredSpringMonth.HasValue && PreferredFallMonth.HasValue)
        {
            var springDate = new DateTime(today.Year, PreferredSpringMonth.Value, 15);
            var fallDate = new DateTime(today.Year, PreferredFallMonth.Value, 15);
            
            if (today < springDate) return springDate;
            if (today < fallDate) return fallDate;
            return new DateTime(today.Year + 1, PreferredSpringMonth.Value, 15);
        }
        
        // If only spring
        if (PreferredSpringMonth.HasValue)
        {
            var springDate = new DateTime(today.Year, PreferredSpringMonth.Value, 15);
            return today < springDate ? springDate : springDate.AddYears(1);
        }
        
        // If only fall
        if (PreferredFallMonth.HasValue)
        {
            var fallDate = new DateTime(today.Year, PreferredFallMonth.Value, 15);
            return today < fallDate ? fallDate : fallDate.AddYears(1);
        }
        
        return null;
    }
}
