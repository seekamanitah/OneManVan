using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Customer entity representing a residential or commercial client.
/// </summary>
public class Customer
{
    public int Id { get; set; }

    /// <summary>
    /// Auto-generated customer number (e.g., C-0001).
    /// </summary>
    [MaxLength(20)]
    public string? CustomerNumber { get; set; }

    // === Basic Info ===
    
    /// <summary>
    /// First name of the customer (individual).
    /// </summary>
    [MaxLength(100)]
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name of the customer (individual).
    /// </summary>
    [MaxLength(100)]
    public string? LastName { get; set; }

    /// <summary>
    /// Full name - computed from FirstName + LastName, or set directly.
    /// For backward compatibility and display purposes.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name
    {
        get
        {
            if (!string.IsNullOrEmpty(FirstName) || !string.IsNullOrEmpty(LastName))
            {
                return $"{FirstName} {LastName}".Trim();
            }
            return _name;
        }
        set => _name = value;
    }
    private string _name = string.Empty;

    /// <summary>
    /// Full name for display purposes (with email if available).
    /// </summary>
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Full name with email for dropdown display.
    /// </summary>
    [NotMapped]
    public string FullNameWithEmail => !string.IsNullOrEmpty(Email)
        ? $"{FullName} ({Email})"
        : FullName;

    /// <summary>
    /// Company name for commercial customers.
    /// </summary>
    [MaxLength(200)]
    public string? CompanyName { get; set; }

    /// <summary>
    /// Foreign key to Company entity (for commercial customers).
    /// </summary>
    public int? CompanyId { get; set; }

    public CustomerType CustomerType { get; set; } = CustomerType.Residential;

    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    // === Primary Contact ===

    [MaxLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(20)]
    [Phone]
    public string? Phone { get; set; }

    /// <summary>
    /// Mobile phone number.
    /// </summary>
    [MaxLength(20)]
    [Phone]
    public string? Mobile { get; set; }

    /// <summary>
    /// Website URL.
    /// </summary>
    [MaxLength(200)]
    public string? Website { get; set; }

    public ContactMethod PreferredContact { get; set; } = ContactMethod.Any;

    // === Secondary Contact ===

    [MaxLength(20)]
    [Phone]
    public string? SecondaryPhone { get; set; }

    [MaxLength(100)]
    [EmailAddress]
    public string? SecondaryEmail { get; set; }

    /// <summary>
    /// Emergency contact name.
    /// </summary>
    [MaxLength(100)]
    public string? EmergencyContact { get; set; }

    [MaxLength(20)]
    [Phone]
    public string? EmergencyPhone { get; set; }

    // === Address ===

    /// <summary>
    /// Home address for residential customers or primary service location.
    /// Used for navigation and service calls.
    /// </summary>
    [MaxLength(500)]
    public string? HomeAddress { get; set; }

    // === Billing & Payment ===

    /// <summary>
    /// Separate billing email if different from primary.
    /// </summary>
    [MaxLength(100)]
    [EmailAddress]
    public string? BillingEmail { get; set; }

    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.DueOnReceipt;

    [Column(TypeName = "decimal(10,2)")]
    public decimal CreditLimit { get; set; } = 0;

    /// <summary>
    /// Current outstanding balance.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal AccountBalance { get; set; } = 0;

    public bool TaxExempt { get; set; } = false;

    [MaxLength(50)]
    public string? TaxExemptId { get; set; }

    // === Marketing & Source ===

    /// <summary>
    /// How the customer found us (Google, Referral, Yard Sign, etc.).
    /// </summary>
    [MaxLength(100)]
    public string? ReferralSource { get; set; }

    /// <summary>
    /// Customer or person who referred them.
    /// </summary>
    [MaxLength(200)]
    public string? ReferredBy { get; set; }

    public DateTime? FirstContactDate { get; set; }

    // === Service Preferences ===

    /// <summary>
    /// Notes about preferred technician or scheduling preferences.
    /// </summary>
    [MaxLength(500)]
    public string? PreferredTechnicianNotes { get; set; }

    /// <summary>
    /// Preferred service window start time.
    /// </summary>
    public TimeSpan? PreferredServiceWindowStart { get; set; }

    /// <summary>
    /// Preferred service window end time.
    /// </summary>
    public TimeSpan? PreferredServiceWindowEnd { get; set; }

    /// <summary>
    /// Standing service instructions visible to technicians.
    /// </summary>
    [MaxLength(1000)]
    public string? ServiceNotes { get; set; }

    // === General Notes ===

    [MaxLength(2000)]
    public string? Notes { get; set; }

    // === Status & Tracking ===

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastServiceDate { get; set; }

    public DateTime? NextScheduledService { get; set; }

    /// <summary>
    /// Total number of completed jobs.
    /// </summary>
    public int TotalJobCount { get; set; } = 0;

    /// <summary>
    /// Lifetime revenue from this customer.
    /// </summary>
    [Column(TypeName = "decimal(12,2)")]
    public decimal LifetimeRevenue { get; set; } = 0;

    // === Tags for Filtering ===

    /// <summary>
    /// Comma-separated tags for filtering (e.g., "VIP,ServiceAgreement,Commercial").
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    // === Navigation Properties ===

    public ICollection<Site> Sites { get; set; } = [];

    public ICollection<Asset> Assets { get; set; } = [];

    // === Computed Properties ===

    [NotMapped]
    public string DisplayName => string.IsNullOrEmpty(CompanyName) 
        ? Name 
        : $"{Name} ({CompanyName})";

    [NotMapped]
    public string CustomerTypeDisplay => CustomerType switch
    {
        CustomerType.Residential => "Residential",
        CustomerType.Commercial => "Commercial",
        CustomerType.PropertyManager => "Property Manager",
        CustomerType.Government => "Government",
        CustomerType.NonProfit => "Non-Profit",
        CustomerType.NewConstruction => "New Construction",
        _ => "Unknown"
    };

    [NotMapped]
    public string StatusDisplay => Status switch
    {
        CustomerStatus.Active => "Active",
        CustomerStatus.Inactive => "Inactive",
        CustomerStatus.Lead => "Lead",
        CustomerStatus.VIP => "VIP",
        CustomerStatus.DoNotService => "Do Not Service",
        CustomerStatus.Delinquent => "Delinquent",
        CustomerStatus.Archived => "Archived",
        _ => "Unknown"
    };

    [NotMapped]
    public List<string> TagList => string.IsNullOrWhiteSpace(Tags)
        ? []
        : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    [NotMapped]
    public bool IsVip => Status == CustomerStatus.VIP || TagList.Contains("VIP", StringComparer.OrdinalIgnoreCase);

    [NotMapped]
    public bool HasOutstandingBalance => AccountBalance > 0;

    [NotMapped]
    public string PreferredServiceWindowDisplay
    {
        get
        {
            if (!PreferredServiceWindowStart.HasValue || !PreferredServiceWindowEnd.HasValue)
                return "Any time";

            var start = DateTime.Today.Add(PreferredServiceWindowStart.Value);
            var end = DateTime.Today.Add(PreferredServiceWindowEnd.Value);
            return $"{start:h:mm tt} - {end:h:mm tt}";
        }
    }

    // New navigation properties for Company relationships
    public Company? Company { get; set; }
    public ICollection<CustomerCompanyRole> CompanyRoles { get; set; } = [];
}

