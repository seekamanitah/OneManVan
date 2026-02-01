using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneManVan.Shared.Models;

/// <summary>
/// Time entry for tracking employee hours worked.
/// </summary>
public class EmployeeTimeLog
{
    public int Id { get; set; }

    /// <summary>
    /// The employee this time entry belongs to.
    /// </summary>
    [Required]
    public int EmployeeId { get; set; }

    /// <summary>
    /// Optional job this time was worked on.
    /// </summary>
    public int? JobId { get; set; }

    /// <summary>
    /// Optional customer this time was worked for.
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// Optional invoice this time log was created from.
    /// </summary>
    public int? InvoiceId { get; set; }

    /// <summary>
    /// Optional invoice line item this time log is linked to.
    /// </summary>
    public int? InvoiceLineItemId { get; set; }

    /// <summary>
    /// Date the work was performed.
    /// </summary>
    public DateTime Date { get; set; } = DateTime.Today;

    /// <summary>
    /// Clock in time (for automatic time logs from invoices).
    /// </summary>
    public DateTime? ClockIn { get; set; }

    /// <summary>
    /// Clock out time (for automatic time logs from invoices).
    /// </summary>
    public DateTime? ClockOut { get; set; }

    /// <summary>
    /// Number of hours worked.
    /// </summary>
    [Column(TypeName = "decimal(6,2)")]
    public decimal HoursWorked { get; set; }

    /// <summary>
    /// Hourly rate for this time entry.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal HourlyRate { get; set; }

    /// <summary>
    /// Total pay for this time entry (HoursWorked * HourlyRate).
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalPay { get; set; }

    /// <summary>
    /// Description of work performed.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Source of time log entry (Manual, Invoice, TimeC lock, etc.).
    /// </summary>
    [MaxLength(50)]
    public string? Source { get; set; }

    /// <summary>
    /// Whether these hours are overtime.
    /// </summary>
    public bool IsOvertime { get; set; } = false;

    /// <summary>
    /// Start time (optional, for time clock style tracking).
    /// </summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// End time (optional, for time clock style tracking).
    /// </summary>
    public TimeSpan? EndTime { get; set; }

    /// <summary>
    /// Break time in minutes.
    /// </summary>
    public int BreakMinutes { get; set; } = 0;

    /// <summary>
    /// Notes about the work performed.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this entry has been approved.
    /// </summary>
    public bool IsApproved { get; set; } = false;

    [MaxLength(100)]
    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Whether this entry has been paid.
    /// </summary>
    public bool IsPaid { get; set; } = false;

    public int? PaymentId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // === Navigation Properties ===

    public Employee Employee { get; set; } = null!;

    public Job? Job { get; set; }

    public Customer? Customer { get; set; }

    public Invoice? Invoice { get; set; }

    public InvoiceLineItem? InvoiceLineItem { get; set; }

    public EmployeePayment? Payment { get; set; }

    // === Computed Properties ===

    [NotMapped]
    public decimal TotalHours => HoursWorked - (BreakMinutes / 60m);

    [NotMapped]
    public string TimeRange => StartTime.HasValue && EndTime.HasValue
        ? $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}"
        : "";
}
