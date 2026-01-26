using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Product catalog entry representing an HVAC equipment template.
/// Products serve as templates with pre-filled specifications that can be
/// quickly applied to customer assets or added to estimates.
/// </summary>
public class Product
{
    public int Id { get; set; }

    /// <summary>
    /// Auto-generated product number (e.g., P-0001).
    /// </summary>
    [MaxLength(20)]
    public string? ProductNumber { get; set; }

    // === Identity ===

    [Required]
    [MaxLength(100)]
    public string Manufacturer { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ModelNumber { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? ProductName { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Key features and selling points (can be displayed on estimates).
    /// </summary>
    [MaxLength(2000)]
    public string? Features { get; set; }

    // === Classification ===

    public ProductCategory Category { get; set; } = ProductCategory.Unknown;

    public EquipmentType EquipmentType { get; set; } = EquipmentType.Unknown;

    public FuelType FuelType { get; set; } = FuelType.Unknown;

    // === HVAC Specifications ===

    public RefrigerantType RefrigerantType { get; set; } = RefrigerantType.Unknown;

    /// <summary>
    /// Factory refrigerant charge in ounces.
    /// </summary>
    [Column(TypeName = "decimal(6,1)")]
    public decimal? RefrigerantChargeOz { get; set; }

    /// <summary>
    /// Tonnage stored as integer (multiply by 10). E.g., 25 = 2.5 ton.
    /// </summary>
    public int? TonnageX10 { get; set; }

    /// <summary>
    /// Cooling capacity in BTU/hr.
    /// </summary>
    public int? CoolingBtu { get; set; }

    /// <summary>
    /// Heating capacity in BTU/hr.
    /// </summary>
    public int? HeatingBtu { get; set; }

    // === Efficiency Ratings ===

    /// <summary>
    /// SEER rating (Seasonal Energy Efficiency Ratio) for AC.
    /// </summary>
    [Column(TypeName = "decimal(4,1)")]
    public decimal? SeerRating { get; set; }

    /// <summary>
    /// SEER2 rating (new 2023 standard).
    /// </summary>
    [Column(TypeName = "decimal(4,1)")]
    public decimal? Seer2Rating { get; set; }

    /// <summary>
    /// EER rating (Energy Efficiency Ratio).
    /// </summary>
    [Column(TypeName = "decimal(4,1)")]
    public decimal? EerRating { get; set; }

    /// <summary>
    /// AFUE rating (Annual Fuel Utilization Efficiency) for furnaces.
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? AfueRating { get; set; }

    /// <summary>
    /// HSPF rating (Heating Seasonal Performance Factor) for heat pumps.
    /// </summary>
    [Column(TypeName = "decimal(4,1)")]
    public decimal? HspfRating { get; set; }

    /// <summary>
    /// HSPF2 rating (new 2023 standard).
    /// </summary>
    [Column(TypeName = "decimal(4,1)")]
    public decimal? Hspf2Rating { get; set; }

    // === Electrical Requirements ===

    /// <summary>
    /// Operating voltage (e.g., 208, 230, 460).
    /// </summary>
    public int? Voltage { get; set; }

    /// <summary>
    /// Phase type (1 or 3).
    /// </summary>
    public int? PhaseType { get; set; }

    /// <summary>
    /// Running load amperage.
    /// </summary>
    [Column(TypeName = "decimal(5,1)")]
    public decimal? Amperage { get; set; }

    /// <summary>
    /// Minimum circuit ampacity.
    /// </summary>
    [Column(TypeName = "decimal(5,1)")]
    public decimal? MinCircuitAmpacity { get; set; }

    /// <summary>
    /// Maximum overcurrent protection (fuse/breaker size).
    /// </summary>
    public int? MaxOvercurrentProtection { get; set; }

    // === Filter Information ===

    [MaxLength(50)]
    public string? FilterSize { get; set; }

    public FilterType FilterType { get; set; } = FilterType.Unknown;

    // === Physical Specifications ===

    /// <summary>
    /// Weight in pounds.
    /// </summary>
    [Column(TypeName = "decimal(6,1)")]
    public decimal? WeightLbs { get; set; }

    /// <summary>
    /// Height in inches.
    /// </summary>
    [Column(TypeName = "decimal(5,1)")]
    public decimal? HeightIn { get; set; }

    /// <summary>
    /// Width in inches.
    /// </summary>
    [Column(TypeName = "decimal(5,1)")]
    public decimal? WidthIn { get; set; }

    /// <summary>
    /// Depth in inches.
    /// </summary>
    [Column(TypeName = "decimal(5,1)")]
    public decimal? DepthIn { get; set; }

    /// <summary>
    /// For split systems - indoor unit model number.
    /// </summary>
    [MaxLength(100)]
    public string? IndoorUnitModel { get; set; }

    /// <summary>
    /// For split systems - outdoor unit model number.
    /// </summary>
    [MaxLength(100)]
    public string? OutdoorUnitModel { get; set; }

    // === Pricing ===

    /// <summary>
    /// Manufacturer's Suggested Retail Price.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? Msrp { get; set; }

    /// <summary>
    /// Your wholesale/cost price.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? WholesaleCost { get; set; }

    /// <summary>
    /// Your suggested selling price.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? SuggestedSellPrice { get; set; }

    /// <summary>
    /// Estimated labor hours for installation.
    /// </summary>
    [Column(TypeName = "decimal(4,1)")]
    public decimal? LaborHoursEstimate { get; set; }

    /// <summary>
    /// Installation notes and requirements.
    /// </summary>
    [MaxLength(2000)]
    public string? InstallationNotes { get; set; }

    // === Warranty ===

    /// <summary>
    /// Parts warranty in years.
    /// </summary>
    public int? PartsWarrantyYears { get; set; }

    /// <summary>
    /// Compressor warranty in years (for AC/Heat Pumps).
    /// </summary>
    public int? CompressorWarrantyYears { get; set; }

    /// <summary>
    /// Heat exchanger warranty in years (for furnaces).
    /// </summary>
    public int? HeatExchangerWarrantyYears { get; set; }

    /// <summary>
    /// Labor warranty in years.
    /// </summary>
    public int? LaborWarrantyYears { get; set; }

    /// <summary>
    /// Whether product registration is required for full warranty.
    /// </summary>
    public bool RegistrationRequired { get; set; } = false;

    // === Status ===

    public bool IsActive { get; set; } = true;

    public bool IsDiscontinued { get; set; } = false;

    public DateTime? DiscontinuedDate { get; set; }

    /// <summary>
    /// Successor model when this product is discontinued.
    /// </summary>
    public int? ReplacementProductId { get; set; }

    // === Metadata ===

    /// <summary>
    /// Manufacturer's part number (may differ from model number).
    /// </summary>
    [MaxLength(100)]
    public string? ManufacturerPartNumber { get; set; }

    /// <summary>
    /// UPC barcode.
    /// </summary>
    [MaxLength(50)]
    public string? UpcCode { get; set; }

    /// <summary>
    /// Link to manufacturer's product page.
    /// </summary>
    [MaxLength(500)]
    public string? ManufacturerUrl { get; set; }

    /// <summary>
    /// Link to product video (YouTube, etc.).
    /// </summary>
    [MaxLength(500)]
    public string? VideoUrl { get; set; }

    /// <summary>
    /// Comma-separated tags for filtering.
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // === Navigation Properties ===

    public Product? ReplacementProduct { get; set; }

    public ICollection<ProductDocument> Documents { get; set; } = [];

    // === Computed Properties ===

    [NotMapped]
    public decimal? Tonnage => TonnageX10.HasValue ? TonnageX10.Value / 10.0m : null;

    [NotMapped]
    public string DisplayName => !string.IsNullOrWhiteSpace(ProductName)
        ? ProductName
        : $"{Manufacturer} {ModelNumber}";

    [NotMapped]
    public string FullDisplayName => $"{Manufacturer} {ModelNumber}" +
        (!string.IsNullOrWhiteSpace(ProductName) ? $" - {ProductName}" : "");

    [NotMapped]
    public string CategoryDisplay => Category switch
    {
        ProductCategory.AirConditioner => "Air Conditioner",
        ProductCategory.HeatPump => "Heat Pump",
        ProductCategory.GasFurnace => "Gas Furnace",
        ProductCategory.OilFurnace => "Oil Furnace",
        ProductCategory.ElectricFurnace => "Electric Furnace",
        ProductCategory.Boiler => "Boiler",
        ProductCategory.MiniSplit => "Mini Split",
        ProductCategory.PackagedUnit => "Packaged Unit",
        ProductCategory.AirHandler => "Air Handler",
        ProductCategory.Coil => "Coil",
        ProductCategory.Thermostat => "Thermostat",
        ProductCategory.AirPurifier => "Air Purifier",
        ProductCategory.Humidifier => "Humidifier",
        ProductCategory.Dehumidifier => "Dehumidifier",
        ProductCategory.Accessories => "Accessories",
        _ => "Unknown"
    };

    [NotMapped]
    public string CapacityDisplay
    {
        get
        {
            if (Tonnage.HasValue) return $"{Tonnage:0.#} Ton";
            if (CoolingBtu.HasValue) return $"{CoolingBtu:N0} BTU";
            if (HeatingBtu.HasValue) return $"{HeatingBtu:N0} BTU";
            return "N/A";
        }
    }

    [NotMapped]
    public string EfficiencyDisplay
    {
        get
        {
            if (Seer2Rating.HasValue) return $"SEER2 {Seer2Rating:0.#}";
            if (SeerRating.HasValue) return $"SEER {SeerRating:0.#}";
            if (AfueRating.HasValue) return $"AFUE {AfueRating:0.#}%";
            if (Hspf2Rating.HasValue) return $"HSPF2 {Hspf2Rating:0.#}";
            if (HspfRating.HasValue) return $"HSPF {HspfRating:0.#}";
            return "";
        }
    }

    [NotMapped]
    public string RefrigerantDisplay => RefrigerantType switch
    {
        Enums.RefrigerantType.R22 => "R-22",
        Enums.RefrigerantType.R410A => "R-410A",
        Enums.RefrigerantType.R407C => "R-407C",
        Enums.RefrigerantType.R134a => "R-134a",
        Enums.RefrigerantType.R32 => "R-32",
        Enums.RefrigerantType.R454B => "R-454B",
        Enums.RefrigerantType.R452B => "R-452B",
        Enums.RefrigerantType.R290 => "R-290",
        _ => ""
    };

    [NotMapped]
    public string ElectricalDisplay
    {
        get
        {
            var parts = new List<string>();
            if (Voltage.HasValue) parts.Add($"{Voltage}V");
            if (PhaseType.HasValue) parts.Add($"{PhaseType}Ph");
            if (Amperage.HasValue) parts.Add($"{Amperage:0.#}A");
            return string.Join(" / ", parts);
        }
    }

    [NotMapped]
    public string DimensionsDisplay
    {
        get
        {
            if (!HeightIn.HasValue && !WidthIn.HasValue && !DepthIn.HasValue)
                return "";
            return $"{HeightIn:0.#}\"H × {WidthIn:0.#}\"W × {DepthIn:0.#}\"D";
        }
    }

    [NotMapped]
    public string WarrantyDisplay
    {
        get
        {
            var parts = new List<string>();
            if (PartsWarrantyYears.HasValue) parts.Add($"Parts: {PartsWarrantyYears}yr");
            if (CompressorWarrantyYears.HasValue) parts.Add($"Comp: {CompressorWarrantyYears}yr");
            if (HeatExchangerWarrantyYears.HasValue) parts.Add($"HX: {HeatExchangerWarrantyYears}yr");
            if (LaborWarrantyYears.HasValue) parts.Add($"Labor: {LaborWarrantyYears}yr");
            return string.Join(", ", parts);
        }
    }

    [NotMapped]
    public decimal? ProfitMargin => WholesaleCost.HasValue && SuggestedSellPrice.HasValue && WholesaleCost > 0
        ? ((SuggestedSellPrice.Value - WholesaleCost.Value) / WholesaleCost.Value) * 100
        : null;

    [NotMapped]
    public List<string> TagList => string.IsNullOrWhiteSpace(Tags)
        ? []
        : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
