using System.ComponentModel.DataAnnotations;

namespace OneManVan.Shared.Models;

/// <summary>
/// Stores manufacturer information and warranty registration URLs.
/// Allows users to configure/update registration websites.
/// </summary>
public class ManufacturerRegistration
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string BrandName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? RegistrationUrl { get; set; }

    [MaxLength(100)]
    public string? SupportPhone { get; set; }

    [MaxLength(200)]
    public string? SupportEmail { get; set; }

    [MaxLength(500)]
    public string? ManufacturerWebsite { get; set; }

    /// <summary>
    /// Notes about warranty registration (e.g., "Must register within 90 days")
    /// </summary>
    [MaxLength(1000)]
    public string? RegistrationNotes { get; set; }

    /// <summary>
    /// Is this brand actively used/shown in dropdowns?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order in brand dropdowns
    /// </summary>
    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
