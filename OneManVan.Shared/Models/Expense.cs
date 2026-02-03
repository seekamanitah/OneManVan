using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Business expense tracking for operational costs, materials, and payments.
/// Can be linked to jobs, invoices, employees, or standalone.
/// </summary>
public class Expense
{
    public int Id { get; set; }

    /// <summary>
    /// Auto-generated expense number (e.g., EXP-0001).
    /// </summary>
    [MaxLength(20)]
    public string? ExpenseNumber { get; set; }

    /// <summary>
    /// Date the expense occurred.
    /// </summary>
    public DateTime ExpenseDate { get; set; } = DateTime.Today;

    /// <summary>
    /// Category of expense.
    /// </summary>
    public ExpenseCategory Category { get; set; } = ExpenseCategory.Other;

    /// <summary>
    /// Brief description of the expense.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Expense amount.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Tax amount if applicable.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal TaxAmount { get; set; } = 0;

    /// <summary>
    /// Total amount (Amount + TaxAmount).
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Vendor/Supplier name.
    /// </summary>
    [MaxLength(200)]
    public string? VendorName { get; set; }

    /// <summary>
    /// Receipt or invoice number from vendor.
    /// </summary>
    [MaxLength(100)]
    public string? ReceiptNumber { get; set; }

    /// <summary>
    /// Payment method used.
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    /// <summary>
    /// Whether this expense is billable to a customer.
    /// </summary>
    public bool IsBillable { get; set; } = false;

    /// <summary>
    /// Whether this expense has been reimbursed (for employee expenses).
    /// </summary>
    public bool IsReimbursed { get; set; } = false;

    /// <summary>
    /// Date reimbursed if applicable.
    /// </summary>
    public DateTime? ReimbursedDate { get; set; }

    /// <summary>
    /// Optional link to job this expense relates to.
    /// </summary>
    public int? JobId { get; set; }

    /// <summary>
    /// Optional link to customer.
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// Optional link to invoice if billed.
    /// </summary>
    public int? InvoiceId { get; set; }

    /// <summary>
    /// Employee who incurred the expense.
    /// </summary>
    public int? EmployeeId { get; set; }

    /// <summary>
    /// Path to receipt image/document.
    /// </summary>
    [MaxLength(500)]
    public string? ReceiptPath { get; set; }

    /// <summary>
    /// Additional notes about the expense.
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Status of the expense.
    /// </summary>
    public ExpenseStatus Status { get; set; } = ExpenseStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("JobId")]
    public virtual Job? Job { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    [ForeignKey("InvoiceId")]
    public virtual Invoice? Invoice { get; set; }

    [ForeignKey("EmployeeId")]
    public virtual Employee? Employee { get; set; }

    // === Computed Properties ===
    
    [NotMapped]
    public string CategoryDisplay => Category.ToString();

    [NotMapped]
    public string StatusDisplay => Status.ToString();
}

/// <summary>
/// Categories for expense tracking.
/// </summary>
public enum ExpenseCategory
{
    Materials = 1,
    Fuel = 2,
    Tools = 3,
    Equipment = 4,
    VehicleMaintenance = 5,
    OfficeSupplies = 6,
    Insurance = 7,
    Licenses = 8,
    Training = 9,
    Subcontractor = 10,
    Utilities = 11,
    Rent = 12,
    Marketing = 13,
    Software = 14,
    Travel = 15,
    Meals = 16,
    Uniforms = 17,
    Safety = 18,
    Other = 99
}

/// <summary>
/// Status of an expense entry.
/// </summary>
public enum ExpenseStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Reimbursed = 4,
    Paid = 5
}
