using System.ComponentModel.DataAnnotations;

namespace OneManVan.Shared.Models;

/// <summary>
/// Company settings and branding information used on invoices, estimates, and other documents.
/// </summary>
public class CompanySettings
{
    [Key]
    public int Id { get; set; } = 1; // Singleton - only one record

    [Required]
    [MaxLength(200)]
    public string CompanyName { get; set; } = "OneManVan";

    [MaxLength(500)]
    public string? Tagline { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(50)]
    public string? State { get; set; }

    [MaxLength(20)]
    public string? ZipCode { get; set; }

    [MaxLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Website { get; set; }

    /// <summary>
    /// Base64 encoded logo image
    /// </summary>
    public string? LogoBase64 { get; set; }

    /// <summary>
    /// Logo file name for reference
    /// </summary>
    [MaxLength(200)]
    public string? LogoFileName { get; set; }

    /// <summary>
    /// Tax ID / EIN number
    /// </summary>
    [MaxLength(50)]
    public string? TaxId { get; set; }

    /// <summary>
    /// License number (trade license, contractor license, etc.)
    /// </summary>
    [MaxLength(100)]
    public string? LicenseNumber { get; set; }

    /// <summary>
    /// Insurance policy number
    /// </summary>
    [MaxLength(100)]
    public string? InsuranceNumber { get; set; }

    /// <summary>
    /// Default payment terms text shown on invoices
    /// </summary>
    [MaxLength(500)]
    public string? PaymentTerms { get; set; } = "Payment due within 30 days of invoice date.";

    /// <summary>
    /// Footer text for invoices/estimates
    /// </summary>
    [MaxLength(500)]
    public string? DocumentFooter { get; set; } = "Thank you for your business!";

    /// <summary>
    /// Bank account details for payments
    /// </summary>
    [MaxLength(500)]
    public string? BankDetails { get; set; }

    /// <summary>
    /// Google Calendar integration settings (JSON)
    /// </summary>
    [MaxLength(4000)]
    public string? GoogleCalendarSettings { get; set; }

    /// <summary>
    /// Maps API key for route optimization
    /// </summary>
    [MaxLength(200)]
    public string? MapsApiKey { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Full formatted address
    /// </summary>
    public string FullAddress => string.Join(", ", 
        new[] { Address, City, State, ZipCode }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
}
