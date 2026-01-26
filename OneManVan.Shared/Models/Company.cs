using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Company entity for commercial customers, vendors, and suppliers.
/// Enables multi-customer relationships and commercial work management.
/// </summary>
public class Company
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? LegalName { get; set; }

    [MaxLength(50)]
    public string? TaxId { get; set; }

    [MaxLength(200)]
    public string? Website { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    // Billing Address
    [MaxLength(200)]
    public string? BillingAddress { get; set; }

    [MaxLength(100)]
    public string? BillingCity { get; set; }

    [MaxLength(2)]
    public string? BillingState { get; set; }

    [MaxLength(10)]
    public string? BillingZipCode { get; set; }

    public CompanyType CompanyType { get; set; } = CompanyType.Customer;

    [MaxLength(100)]
    public string? Industry { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Customer> Customers { get; set; } = [];
    public ICollection<Asset> Assets { get; set; } = [];
    public ICollection<CustomerCompanyRole> CustomerRoles { get; set; } = [];
    public ICollection<AssetOwner> OwnedAssets { get; set; } = [];
}
