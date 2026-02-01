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

    // === Service Tier (HVAC Specific) ===

    /// <summary>
    /// Service tier level (Basic, Standard, Premium).
    /// </summary>
    public ServiceTier ServiceTier { get; set; } = ServiceTier.Standard;

    /// <summary>
    /// No emergency dispatch fee (Standard+Premium).
    /// </summary>
    public bool NoEmergencyDispatchFee { get; set; } = false;

    /// <summary>
    /// Free minor adjustments during tune-ups (Premium only).
    /// Includes belts, small capacitor test/replace.
    /// </summary>
    public bool FreeMinorAdjustments { get; set; } = false;

    /// <summary>
    /// Number of filters included per year (Premium: 2).
    /// </summary>
    public int FiltersIncludedPerYear { get; set; } = 0;

    /// <summary>
    /// Additional priority check visits per year (Premium: 1).
    /// </summary>
    public int AdditionalCheckVisits { get; set; } = 0;

    /// <summary>
    /// Priority response time in hours (Standard: 24, Premium: 12).
    /// </summary>
    public int ResponseTimeHours { get; set; } = 48;

    /// <summary>
    /// Spring AC tune-up tasks (JSON list).
    /// </summary>
    [MaxLength(2000)]
    public string? SpringAcTuneUpTasks { get; set; }

    /// <summary>
    /// Fall heating tune-up tasks (JSON list).
    /// </summary>
    [MaxLength(2000)]
    public string? FallHeatingTuneUpTasks { get; set; }

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

    [NotMapped]
    public string ServiceTierDisplay => ServiceTier switch
    {
        ServiceTier.Basic => "Basic",
        ServiceTier.Standard => "Standard",
        ServiceTier.Premium => "Premium",
        ServiceTier.Custom => "Custom",
        _ => ServiceTier.ToString()
    };

    /// <summary>
    /// Configures agreement settings based on selected service tier.
    /// </summary>
    public void ApplyTierDefaults()
    {
        switch (ServiceTier)
        {
            case ServiceTier.Basic:
                IncludedVisitsPerYear = 1;
                RepairDiscountPercent = 10;
                PriorityService = true;
                WaiveTripCharge = false;
                NoEmergencyDispatchFee = false;
                FreeMinorAdjustments = false;
                FiltersIncludedPerYear = 0;
                AdditionalCheckVisits = 0;
                ResponseTimeHours = 48;
                IncludesAcTuneUp = true;
                IncludesHeatingTuneUp = false; // Customer chooses one
                break;

            case ServiceTier.Standard:
                IncludedVisitsPerYear = 2;
                RepairDiscountPercent = 15;
                PriorityService = true;
                WaiveTripCharge = true;
                NoEmergencyDispatchFee = true;
                FreeMinorAdjustments = false;
                FiltersIncludedPerYear = 0;
                AdditionalCheckVisits = 0;
                ResponseTimeHours = 24;
                IncludesAcTuneUp = true;
                IncludesHeatingTuneUp = true;
                break;

            case ServiceTier.Premium:
                IncludedVisitsPerYear = 2;
                RepairDiscountPercent = 20;
                PriorityService = true;
                WaiveTripCharge = true;
                NoEmergencyDispatchFee = true;
                FreeMinorAdjustments = true;
                FiltersIncludedPerYear = 2;
                AdditionalCheckVisits = 1;
                ResponseTimeHours = 12;
                IncludesAcTuneUp = true;
                IncludesHeatingTuneUp = true;
                break;

            case ServiceTier.Custom:
                // Don't modify - user configures manually
                break;
        }

        // Set default tune-up task lists
        SetDefaultTuneUpTasks();
    }

    /// <summary>
    /// Sets default Spring AC and Fall Heating tune-up task lists.
    /// </summary>
    public void SetDefaultTuneUpTasks()
    {
        if (string.IsNullOrEmpty(SpringAcTuneUpTasks))
        {
            SpringAcTuneUpTasks = System.Text.Json.JsonSerializer.Serialize(new[]
            {
                "Clean evaporator & condenser coils",
                "Check refrigerant charge & pressures",
                "Inspect/clean filters",
                "Test electrical components & capacitors",
                "Calibrate thermostat",
                "Check blower & airflow",
                "Clear condensate drain"
            });
        }

        if (string.IsNullOrEmpty(FallHeatingTuneUpTasks))
        {
            FallHeatingTuneUpTasks = System.Text.Json.JsonSerializer.Serialize(new[]
            {
                "Inspect heat exchanger for cracks",
                "Clean burners/ignition assembly",
                "Check gas pressure & flue/vent",
                "Inspect belts/pulleys",
                "Measure temperature rise",
                "Test safety controls"
            });
        }
    }

    /// <summary>
    /// Gets the Spring AC tune-up tasks as a list.
    /// </summary>
    [NotMapped]
    public List<string> SpringAcTaskList
    {
        get
        {
            if (string.IsNullOrEmpty(SpringAcTuneUpTasks)) return new();
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(SpringAcTuneUpTasks) ?? new();
            }
            catch
            {
                return new();
            }
        }
    }

    /// <summary>
    /// Gets the Fall Heating tune-up tasks as a list.
    /// </summary>
    [NotMapped]
    public List<string> FallHeatingTaskList
    {
        get
        {
            if (string.IsNullOrEmpty(FallHeatingTuneUpTasks)) return new();
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(FallHeatingTuneUpTasks) ?? new();
            }
            catch
            {
                return new();
            }
        }
    }
}
