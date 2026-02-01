using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Employee or contractor entity for tracking workers, subcontractors, 
/// their hours, payments, and performance.
/// </summary>
public class Employee
{
    public int Id { get; set; }

    // === Basic Information ===

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Type of worker (Full-Time, Part-Time, 1099 Contractor, Subcontractor Company).
    /// </summary>
    public EmployeeType Type { get; set; } = EmployeeType.FullTime;

    /// <summary>
    /// Current employment status.
    /// </summary>
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    public DateTime StartDate { get; set; } = DateTime.Today;

    public DateTime? TerminationDate { get; set; }

    // === Contact Information ===

    [MaxLength(200)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(50)]
    public string? State { get; set; }

    [MaxLength(20)]
    public string? ZipCode { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    [EmailAddress]
    public string? Email { get; set; }

    // === Emergency Contact ===

    [MaxLength(100)]
    public string? EmergencyContactName { get; set; }

    [MaxLength(20)]
    public string? EmergencyContactPhone { get; set; }

    [MaxLength(50)]
    public string? EmergencyContactRelationship { get; set; }

    // === Sensitive Information (should be encrypted in production) ===

    /// <summary>
    /// SSN for employees, EIN for contractors (encrypted storage recommended).
    /// </summary>
    [MaxLength(50)]
    public string? TaxId { get; set; }

    /// <summary>
    /// Whether the TaxId is an EIN (true) or SSN (false).
    /// </summary>
    public bool IsEin { get; set; } = false;

    // === Pay Information ===

    /// <summary>
    /// How the worker is paid (Hourly, Flat per job, Salary).
    /// </summary>
    public PayRateType PayRateType { get; set; } = PayRateType.Hourly;

    /// <summary>
    /// Pay rate amount (per hour, per job, or annual salary).
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal PayRate { get; set; }

    /// <summary>
    /// Overtime rate multiplier (e.g., 1.5 for time-and-a-half).
    /// </summary>
    [Column(TypeName = "decimal(4,2)")]
    public decimal OvertimeMultiplier { get; set; } = 1.5m;

    /// <summary>
    /// Hours per week before overtime kicks in.
    /// </summary>
    public int OvertimeThresholdHours { get; set; } = 40;

    /// <summary>
    /// How the worker is paid.
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.DirectDeposit;

    // === PTO & Hours ===

    /// <summary>
    /// Current PTO balance in hours.
    /// </summary>
    [Column(TypeName = "decimal(8,2)")]
    public decimal PtoBalanceHours { get; set; } = 0;

    /// <summary>
    /// Annual PTO accrual in hours.
    /// </summary>
    [Column(TypeName = "decimal(8,2)")]
    public decimal PtoAccrualPerYear { get; set; } = 0;

    // === Last Payment Info ===

    public DateTime? LastPaidDate { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? LastPaidAmount { get; set; }

    [MaxLength(50)]
    public string? LastPaidMethod { get; set; }

    // === Subcontractor Company Specific ===

    /// <summary>
    /// Company name (for SubcontractorCompany type).
    /// </summary>
    [MaxLength(200)]
    public string? CompanyName { get; set; }

    /// <summary>
    /// Contact person at the subcontractor company.
    /// </summary>
    [MaxLength(100)]
    public string? ContactPerson { get; set; }

    /// <summary>
    /// EIN for subcontractor company.
    /// </summary>
    [MaxLength(20)]
    public string? CompanyEin { get; set; }

    /// <summary>
    /// Services provided by the subcontractor.
    /// </summary>
    [MaxLength(500)]
    public string? ServiceProvided { get; set; }

    /// <summary>
    /// Whether invoices are required from this subcontractor.
    /// </summary>
    public bool InvoiceRequired { get; set; } = false;

    /// <summary>
    /// Whether insurance certificate is on file.
    /// </summary>
    public bool InsuranceOnFile { get; set; } = false;

    // === Classification & Documents ===

    public bool HasW4 { get; set; } = false;
    public int? W4DocumentId { get; set; }

    public bool HasI9 { get; set; } = false;
    public int? I9DocumentId { get; set; }

    public bool Has1099Setup { get; set; } = false;
    public int? W9DocumentId { get; set; }

    public int? InsuranceCertDocumentId { get; set; }

    public DateTime? BackgroundCheckDate { get; set; }

    [MaxLength(50)]
    public string? BackgroundCheckResult { get; set; }

    public DateTime? DrugTestDate { get; set; }

    [MaxLength(50)]
    public string? DrugTestResult { get; set; }

    // === Notes ===

    /// <summary>
    /// General notes about the employee.
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    // === Photo ===

    [MaxLength(500)]
    public string? PhotoPath { get; set; }

    // === Tracking ===

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // === Navigation Properties ===

    /// <summary>
    /// Time entries logged by this employee.
    /// </summary>
    public ICollection<EmployeeTimeLog> TimeLogs { get; set; } = new List<EmployeeTimeLog>();

    /// <summary>
    /// Performance notes for this employee.
    /// </summary>
    public ICollection<EmployeePerformanceNote> PerformanceNotes { get; set; } = new List<EmployeePerformanceNote>();

    /// <summary>
    /// Payment history for this employee.
    /// </summary>
    public ICollection<EmployeePayment> Payments { get; set; } = new List<EmployeePayment>();

    // === Computed Properties ===

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();

    [NotMapped]
    public string DisplayName => Type == EmployeeType.SubcontractorCompany && !string.IsNullOrEmpty(CompanyName)
        ? CompanyName
        : FullName;

    [NotMapped]
    public string TypeDisplay => Type switch
    {
        EmployeeType.FullTime => "Full-Time",
        EmployeeType.PartTime => "Part-Time",
        EmployeeType.Contractor1099 => "1099 Contractor",
        EmployeeType.SubcontractorCompany => "Subcontractor Company",
        _ => Type.ToString()
    };

    [NotMapped]
    public string StatusDisplay => Status switch
    {
        EmployeeStatus.Active => "Active",
        EmployeeStatus.Inactive => "Inactive",
        EmployeeStatus.Terminated => "Terminated",
        EmployeeStatus.OnLeave => "On Leave",
        _ => Status.ToString()
    };

    [NotMapped]
    public string PayRateDisplay => PayRateType switch
    {
        PayRateType.Hourly => $"${PayRate:N2}/hr",
        PayRateType.Flat => $"${PayRate:N2} flat",
        PayRateType.Salary => $"${PayRate:N2}/year",
        _ => $"${PayRate:N2}"
    };

    [NotMapped]
    public bool IsSubcontractor => Type == EmployeeType.SubcontractorCompany;

    [NotMapped]
    public bool Is1099 => Type == EmployeeType.Contractor1099 || Type == EmployeeType.SubcontractorCompany;

    [NotMapped]
    public int TenureDays => (DateTime.Today - StartDate).Days;

    [NotMapped]
    public string TenureDisplay
    {
        get
        {
            var years = TenureDays / 365;
            var months = (TenureDays % 365) / 30;
            if (years > 0) return $"{years}y {months}m";
            if (months > 0) return $"{months} months";
            return $"{TenureDays} days";
        }
    }
}
