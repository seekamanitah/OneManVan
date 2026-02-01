using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Represents a system/zone within a material list.
/// Allows multiple HVAC systems per material list (e.g., "System 1 - Main Floor", "System 2 - Addition").
/// </summary>
public class MaterialListSystem
{
    public int Id { get; set; }

    /// <summary>
    /// Parent material list.
    /// </summary>
    [Required]
    public int MaterialListId { get; set; }

    /// <summary>
    /// Display name for this system/zone (e.g., "Main Floor", "Upstairs", "Addition").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "System 1";

    /// <summary>
    /// Order for display (1, 2, 3...).
    /// </summary>
    public int SortOrder { get; set; } = 1;

    // === System Specifications ===

    /// <summary>
    /// Type of HVAC system.
    /// </summary>
    public HvacSystemType SystemType { get; set; } = HvacSystemType.SplitSystem;

    /// <summary>
    /// Duct system design (Spider vs Trunk).
    /// </summary>
    public bool IsTrunkSystem { get; set; } = false;

    /// <summary>
    /// Plenum material for spider systems.
    /// </summary>
    public PlenumMaterial? PlenumMaterial { get; set; }

    /// <summary>
    /// Main trunk size in inches (for trunk systems).
    /// </summary>
    public int? MainTrunkSize { get; set; }

    // === Equipment Details ===

    /// <summary>
    /// Equipment model number.
    /// </summary>
    [MaxLength(100)]
    public string? EquipmentModel { get; set; }

    /// <summary>
    /// System tonnage (1.5, 2, 2.5, 3, 3.5, 4, 5).
    /// </summary>
    [Column(TypeName = "decimal(3,1)")]
    public decimal? Tonnage { get; set; }

    /// <summary>
    /// SEER rating.
    /// </summary>
    public int? SeerRating { get; set; }

    /// <summary>
    /// BTU rating for furnaces.
    /// </summary>
    public int? BtuRating { get; set; }

    // === Square Footage for this zone ===

    /// <summary>
    /// Square footage covered by this system.
    /// </summary>
    public int? SquareFootage { get; set; }

    // === Notes ===

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // === Navigation Properties ===

    public MaterialList MaterialList { get; set; } = null!;

    /// <summary>
    /// Items belonging to this system.
    /// </summary>
    public ICollection<MaterialListItem> Items { get; set; } = [];

    // === Computed Properties ===

    [NotMapped]
    public string SystemTypeDisplay => SystemType switch
    {
        HvacSystemType.Spider => "Spider System",
        HvacSystemType.Trunk => "Trunk & Branch",
        HvacSystemType.HeatPump => "Heat Pump",
        HvacSystemType.GasFurnace => "Gas Furnace",
        HvacSystemType.SplitSystem => "Split System",
        HvacSystemType.PackageUnit => "Package Unit",
        HvacSystemType.MiniSplit => "Mini-Split",
        _ => SystemType.ToString()
    };

    [NotMapped]
    public string DuctSystemDisplay => IsTrunkSystem ? "Trunk & Branch" : "Spider/Radial";

    [NotMapped]
    public decimal TotalCost => Items.Sum(i => i.TotalCost);

    [NotMapped]
    public int ItemCount => Items.Count;
}
