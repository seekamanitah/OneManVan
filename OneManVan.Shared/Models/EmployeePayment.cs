using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Payment record for employee/contractor compensation.
/// </summary>
public class EmployeePayment
{
    public int Id { get; set; }

    /// <summary>
    /// The employee this payment is for.
    /// </summary>
    [Required]
    public int EmployeeId { get; set; }

    /// <summary>
    /// Date of the payment.
    /// </summary>
    public DateTime PayDate { get; set; } = DateTime.Today;

    /// <summary>
    /// Gross payment amount.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal GrossAmount { get; set; }

    /// <summary>
    /// Total deductions.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal Deductions { get; set; } = 0;

    /// <summary>
    /// Net payment amount (GrossAmount - Deductions).
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal NetAmount { get; set; }

    /// <summary>
    /// Method of payment.
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// Start of pay period.
    /// </summary>
    public DateTime? PeriodStart { get; set; }

    /// <summary>
    /// End of pay period.
    /// </summary>
    public DateTime? PeriodEnd { get; set; }

    /// <summary>
    /// Regular hours paid.
    /// </summary>
    [Column(TypeName = "decimal(8,2)")]
    public decimal RegularHours { get; set; } = 0;

    /// <summary>
    /// Overtime hours paid.
    /// </summary>
    [Column(TypeName = "decimal(8,2)")]
    public decimal OvertimeHours { get; set; } = 0;

    /// <summary>
    /// Regular pay amount.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal RegularPay { get; set; } = 0;

    /// <summary>
    /// Overtime pay amount.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal OvertimePay { get; set; } = 0;

    /// <summary>
    /// Check number, ACH reference, or invoice number.
    /// </summary>
    [MaxLength(50)]
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Notes about this payment.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this is a bonus or special payment.
    /// </summary>
    public bool IsBonus { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // === Navigation Properties ===

    public Employee Employee { get; set; } = null!;

    /// <summary>
    /// Time logs included in this payment.
    /// </summary>
    public ICollection<EmployeeTimeLog> TimeLogs { get; set; } = new List<EmployeeTimeLog>();

    // === Computed Properties ===

    [NotMapped]
    public decimal TotalHours => RegularHours + OvertimeHours;

    [NotMapped]
    public string PaymentMethodDisplay => PaymentMethod switch
    {
        PaymentMethod.DirectDeposit => "Direct Deposit",
        PaymentMethod.Check => "Check",
        PaymentMethod.ACH => "ACH",
        PaymentMethod.Invoice => "Invoice",
        PaymentMethod.Cash => "Cash",
        _ => PaymentMethod.ToString()
    };

    [NotMapped]
    public string PeriodDisplay => PeriodStart.HasValue && PeriodEnd.HasValue
        ? $"{PeriodStart:MMM dd} - {PeriodEnd:MMM dd, yyyy}"
        : PayDate.ToString("MMM dd, yyyy");
}
