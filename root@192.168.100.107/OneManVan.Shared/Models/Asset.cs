using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Asset entity representing HVAC equipment at a customer site.
/// Assets can be associated with either a Site (preferred for property-based tracking)
/// or a Customer (for portable equipment or when site is unknown).
/// At least one of CustomerId or SiteId must be provided.
/// </summary>
public class Asset
{
    public int Id { get; set; }

    /// <summary>
    /// Friendly asset name (e.g., "Main Office RTU", "Lobby AC Unit").
    /// </summary>
    [MaxLength(200)]
    public string? AssetName { get; set; }

    /// <summary>
    /// Detailed description of the asset.
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Optional customer reference. Use SiteId when equipment stays with property.
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// Optional company reference for commercial/multi-asset ownership.
    /// </summary>
    public int? CompanyId { get; set; }

    /// <summary>
    /// Optional site/location reference. Preferred for property-based equipment tracking.
    /// Allows equipment to stay with property when ownership changes.
    /// </summary>
    public int? SiteId { get; set; }

    /// <summary>
    /// Auto-generated asset tag for barcode/QR scanning.
    /// </summary>
    [MaxLength(50)]
    public string? AssetTag { get; set; }

    // === Identity ===

    [Required]
    [MaxLength(100)]
    public string Serial { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Brand { get; set; }

    [MaxLength(100)]
    public string? Model { get; set; }

    /// <summary>
    /// Friendly name like "Upstairs Unit", "Main Floor", "Basement".
    /// This is the physical location nickname.
    /// </summary>
    [MaxLength(100)]
    public string? Nickname { get; set; }

    // === HVAC Configuration ===

    public EquipmentType EquipmentType { get; set; } = EquipmentType.Unknown;

    public FuelType FuelType { get; set; } = FuelType.Unknown;

    public UnitConfig UnitConfig { get; set; } = UnitConfig.Unknown;

    // === Capacity & Efficiency ===

    public int? BtuRating { get; set; }

    /// <summary>
    /// Tonnage stored as integer (multiply by 10). E.g., 25 = 2.5 ton.
    /// </summary>
    public int? TonnageX10 { get; set; }

    /// <summary>
    /// SEER rating (AC efficiency, legacy standard).
    /// </summary>
    [Column(TypeName = "decimal(4,1)")]
    public decimal? SeerRating { get; set; }

    /// <summary>
    /// SEER2 rating (new 2023 standard).
    /// </summary>
    [Column(TypeName = "decimal(4,1)")]
    public decimal? Seer2Rating { get; set; }

    /// <summary>
    /// AFUE rating (furnace efficiency percentage).
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? AfueRating { get; set; }

    /// <summary>
    /// HSPF rating (heat pump heating efficiency, legacy).
    /// </summary>
    [Column(TypeName = "decimal(4,1)")]
    public decimal? HspfRating { get; set; }

    /// <summary>
    /// HSPF2 rating (heat pump, new 2023 standard).
    /// </summary>
    [Column(TypeName = "decimal(4,1)")]
    public decimal? Hspf2Rating { get; set; }

    /// <summary>
    /// EER rating (energy efficiency ratio).
    /// </summary>
    [Column(TypeName = "decimal(4,1)")]
    public decimal? EerRating { get; set; }

    /// <summary>
    /// Electrical voltage (120, 240, 208, etc.).
    /// </summary>
    public int? Voltage { get; set; }

    /// <summary>
    /// Single or Three phase.
    /// </summary>
    [MaxLength(20)]
    public string? PhaseType { get; set; }

    // === Refrigerant (CRITICAL for service) ===

    public RefrigerantType RefrigerantType { get; set; } = RefrigerantType.Unknown;

    /// <summary>
    /// Factory refrigerant charge in ounces.
    /// </summary>
    [Column(TypeName = "decimal(6,1)")]
    public decimal? RefrigerantChargeOz { get; set; }

    // === Filter Information ===

    /// <summary>
    /// Filter size (e.g., "16x25x1", "20x20x4").
    /// </summary>
    [MaxLength(50)]
    public string? FilterSize { get; set; }

    public FilterType FilterType { get; set; } = FilterType.Unknown;

    /// <summary>
    /// Recommended filter change interval in months.
    /// </summary>
    public int? FilterChangeMonths { get; set; }

    public DateTime? LastFilterChange { get; set; }

    public DateTime? NextFilterDue { get; set; }

    // === Thermostat ===

    [MaxLength(50)]
    public string? ThermostatBrand { get; set; }

    [MaxLength(50)]
    public string? ThermostatModel { get; set; }

    public ThermostatType ThermostatType { get; set; } = ThermostatType.Unknown;

    public bool HasWifi { get; set; } = false;


    // === Installation & Warranty ===

    public DateTime? ManufactureDate { get; set; }

    /// <summary>
    /// Installation date of the equipment.
    /// </summary>
    public DateTime? InstallDate { get; set; }

    [MaxLength(200)]
    public string? InstalledBy { get; set; }

    /// <summary>
    /// Is this asset warrantied by SEHVAC?
    /// </summary>
    public bool IsWarrantiedBySEHVAC { get; set; } = false;

    public DateTime? WarrantyStartDate { get; set; }

    /// <summary>
    /// Warranty expiration date (can be set directly or computed).
    /// </summary>
    public DateTime? WarrantyExpiration { get; set; }

    public int WarrantyTermYears { get; set; } = 10;

    /// <summary>
    /// Parts warranty in years (typically 10).
    /// </summary>
    public int PartsWarrantyYears { get; set; } = 10;

    /// <summary>
    /// Labor warranty in years (typically 1).
    /// </summary>
    public int LaborWarrantyYears { get; set; } = 1;

    /// <summary>
    /// Has equipment been registered online for warranty?
    /// Critical for ensuring full warranty coverage.
    /// </summary>
    public bool IsRegisteredOnline { get; set; } = false;

    /// <summary>
    /// Date when equipment was registered online with manufacturer.
    /// </summary>
    public DateTime? RegistrationDate { get; set; }

    /// <summary>
    /// Registration confirmation number or URL from manufacturer.
    /// </summary>
    [MaxLength(200)]
    public string? RegistrationConfirmation { get; set; }

    /// <summary>
    /// Compressor warranty in years (often 10).
    /// </summary>
    public int CompressorWarrantyYears { get; set; } = 10;

    public bool HasExtendedWarranty { get; set; } = false;

    public DateTime? ExtendedWarrantyEnd { get; set; }

    [MaxLength(500)]
    public string? WarrantyNotes { get; set; }

    /// <summary>
    /// Physical location of the asset (room, building, floor).
    /// </summary>
    [MaxLength(200)]
    public string? Location { get; set; }

    // === Financial ===

    /// <summary>
    /// Original purchase price for replacement planning.
    /// </summary>

    [Column(TypeName = "decimal(10,2)")]
    public decimal? PurchasePrice { get; set; }

    /// <summary>
    /// Estimated cost to replace.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? EstimatedReplacementCost { get; set; }

    /// <summary>
    /// Expected useful life in years.
    /// </summary>
    public int? ExpectedLifeYears { get; set; }

    // === Service History ===

    public DateTime? LastServiceDate { get; set; }

    /// <summary>
    /// Recommended maintenance interval in months.
    /// </summary>
    public int? ServiceIntervalMonths { get; set; }

    public DateTime? NextServiceDue { get; set; }

    public AssetCondition Condition { get; set; } = AssetCondition.Unknown;

    [MaxLength(500)]
    public string? ConditionNotes { get; set; }

    // === Technical Notes ===

    [MaxLength(1000)]
    public string? DuctworkNotes { get; set; }

    [MaxLength(1000)]
    public string? TechnicalNotes { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(500)]
    public string? PhotoPath { get; set; }

    // === Status ===

    public AssetStatus Status { get; set; } = AssetStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DecommissionedDate { get; set; }

    /// <summary>
    /// ID of asset that replaced this one.
    /// </summary>
    public int? ReplacedByAssetId { get; set; }

    // === Navigation Properties ===

    public Customer Customer { get; set; } = null!;

    public Site? Site { get; set; }

    public Asset? ReplacedByAsset { get; set; }

    public ICollection<CustomField> CustomFields { get; set; } = [];

    // === Computed Properties ===

    [NotMapped]
    public DateTime? WarrantyEndDate => WarrantyStartDate?.AddYears(WarrantyTermYears);

    [NotMapped]
    public DateTime? PartsWarrantyEnd => WarrantyStartDate?.AddYears(PartsWarrantyYears);

    [NotMapped]
    public DateTime? LaborWarrantyEnd => WarrantyStartDate?.AddYears(LaborWarrantyYears);

    [NotMapped]
    public DateTime? CompressorWarrantyEnd => WarrantyStartDate?.AddYears(CompressorWarrantyYears);

    [NotMapped]
    public bool IsWarrantyExpired => WarrantyEndDate.HasValue && WarrantyEndDate.Value < DateTime.Today;

    [NotMapped]
    public bool IsWarrantyExpiringSoon => WarrantyEndDate.HasValue && 
        !IsWarrantyExpired && 
        WarrantyEndDate.Value <= DateTime.Today.AddDays(90);

    [NotMapped]
    public int? DaysUntilWarrantyExpires => WarrantyEndDate.HasValue
        ? (int)(WarrantyEndDate.Value - DateTime.Today).TotalDays
        : null;

    [NotMapped]
    public decimal? Tonnage => TonnageX10.HasValue ? TonnageX10.Value / 10.0m : null;

    [NotMapped]
    public int? AgeYears => InstallDate.HasValue
        ? (int)((DateTime.Today - InstallDate.Value).TotalDays / 365.25)
        : null;

    [NotMapped]
    public string DisplayName => !string.IsNullOrWhiteSpace(Nickname)
        ? Nickname
        : $"{Brand} {Model}".Trim();

    [NotMapped]
    public string EquipmentTypeDisplay => EquipmentType switch
    {
        Enums.EquipmentType.GasFurnace => "Gas Furnace",
        Enums.EquipmentType.OilFurnace => "Oil Furnace",
        Enums.EquipmentType.ElectricFurnace => "Electric Furnace",
        Enums.EquipmentType.Boiler => "Boiler",
        Enums.EquipmentType.AirConditioner => "Air Conditioner",
        Enums.EquipmentType.HeatPump => "Heat Pump",
        Enums.EquipmentType.MiniSplit => "Mini Split",
        Enums.EquipmentType.DuctlessMiniSplit => "Ductless Mini Split",
        Enums.EquipmentType.PackagedUnit => "Packaged Unit",
        Enums.EquipmentType.RooftopUnit => "Rooftop Unit",
        Enums.EquipmentType.Coil => "Coil",
        Enums.EquipmentType.Condenser => "Condenser",
        Enums.EquipmentType.AirHandler => "Air Handler",
        Enums.EquipmentType.Humidifier => "Humidifier",
        Enums.EquipmentType.Dehumidifier => "Dehumidifier",
        Enums.EquipmentType.AirPurifier => "Air Purifier",
        Enums.EquipmentType.UVLight => "UV Light",
        Enums.EquipmentType.Thermostat => "Thermostat",
        _ => EquipmentType.ToString()
    };

    [NotMapped]
    public string RefrigerantTypeDisplay => RefrigerantType switch
    {
        Enums.RefrigerantType.R22 => "R-22 (Legacy)",
        Enums.RefrigerantType.R410A => "R-410A",
        Enums.RefrigerantType.R407C => "R-407C",
        Enums.RefrigerantType.R134a => "R-134a",
        Enums.RefrigerantType.R32 => "R-32",
        Enums.RefrigerantType.R454B => "R-454B (XL41)",
        Enums.RefrigerantType.R452B => "R-452B (XL55)",
        Enums.RefrigerantType.R290 => "R-290 (Propane)",
        Enums.RefrigerantType.Unknown => "Unknown",
        _ => RefrigerantType.ToString()
    };

    [NotMapped]
    public bool IsLegacyRefrigerant => RefrigerantType == RefrigerantType.R22;

    [NotMapped]
    public string ConditionDisplay => Condition switch
    {
        AssetCondition.Excellent => "Excellent",
        AssetCondition.Good => "Good",
        AssetCondition.Fair => "Fair",
        AssetCondition.Poor => "Poor",
        AssetCondition.FailureImminent => "Failure Imminent",
        AssetCondition.Failed => "Failed",
        _ => "Unknown"
    };

    [NotMapped]
    public bool IsFilterDue => NextFilterDue.HasValue && NextFilterDue.Value <= DateTime.Today;

    [NotMapped]
    public bool IsServiceDue => NextServiceDue.HasValue && NextServiceDue.Value <= DateTime.Today;

    [NotMapped]
    public string CapacitySummary
    {
        get
        {
            var parts = new List<string>();
            if (Tonnage.HasValue) parts.Add($"{Tonnage:0.#} Ton");
            if (BtuRating.HasValue) parts.Add($"{BtuRating:N0} BTU");
            return parts.Count > 0 ? string.Join(" / ", parts) : "N/A";
        }
    }

    [NotMapped]
    public string EfficiencySummary
    {
        get
        {
            var parts = new List<string>();
            if (SeerRating.HasValue || Seer2Rating.HasValue)
            {
                var rating = Seer2Rating ?? SeerRating;
                parts.Add($"SEER {rating:0.#}");
            }
            if (AfueRating.HasValue) parts.Add($"AFUE {AfueRating:0.#}%");
            if (HspfRating.HasValue || Hspf2Rating.HasValue)
            {
                var rating = Hspf2Rating ?? HspfRating;
                parts.Add($"HSPF {rating:0.#}");
            }
            return parts.Count > 0 ? string.Join(" / ", parts) : "N/A";
        }
    }
    
    /// <summary>
    /// Validates that at least Customer or Site is specified.
    /// </summary>
    [NotMapped]
    public bool HasValidLocation => CustomerId.HasValue || SiteId.HasValue;
    
    /// <summary>
    /// Gets the location description for display.
    /// </summary>
    [NotMapped]
    public string LocationDescription => SiteId.HasValue 
        ? (Site?.Address ?? "Site") 
        : (Customer?.Name ?? "Customer");

    // New navigation properties for multi-ownership
    public Company? Company { get; set; }
    public ICollection<AssetOwner> Owners { get; set; } = [];
}

