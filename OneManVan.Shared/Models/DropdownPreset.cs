using System.ComponentModel.DataAnnotations;

namespace OneManVan.Shared.Models;

/// <summary>
/// Stores configurable dropdown preset values that users can customize.
/// Allows adding/removing options for manufacturers, refrigerant types, etc.
/// </summary>
public class DropdownPreset
{
    public int Id { get; set; }

    /// <summary>
    /// Category of the preset (e.g., "Manufacturer", "RefrigerantType", "WarrantyLength").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Display value shown in dropdowns.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string DisplayValue { get; set; } = string.Empty;

    /// <summary>
    /// Optional stored value (if different from display).
    /// </summary>
    [MaxLength(200)]
    public string? StoredValue { get; set; }

    /// <summary>
    /// Sort order for display in dropdowns.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether this preset is active and should appear in dropdowns.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is a system default that cannot be deleted.
    /// </summary>
    public bool IsSystemDefault { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Known preset categories for type safety.
/// </summary>
public static class PresetCategories
{
    public const string Manufacturer = "Manufacturer";
    public const string RefrigerantType = "RefrigerantType";
    public const string WarrantyLength = "WarrantyLength";
    public const string FuelType = "FuelType";
    public const string EquipmentType = "EquipmentType";
    public const string PaymentTerms = "PaymentTerms";
    public const string DocumentCategory = "DocumentCategory";
}
