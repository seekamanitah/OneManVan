using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Invoice entity for billing customers.
/// </summary>
public class Invoice
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public int? JobId { get; set; }

    public int? EstimateId { get; set; }

    [Required]
    public int CustomerId { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

    public DateTime DueDate { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal LaborAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal PartsAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal OtherAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal TaxRate { get; set; } = 7.0m;

    [Column(TypeName = "decimal(10,2)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal AmountPaid { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(2000)]
    public string? Terms { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Job? Job { get; set; }

    public Estimate? Estimate { get; set; }

    public Customer Customer { get; set; } = null!;

    public ICollection<Payment> Payments { get; set; } = [];

    public decimal BalanceDue => Total - AmountPaid;

    public bool IsPaid => AmountPaid >= Total && Total > 0;

    public bool IsOverdue => !IsPaid && DueDate < DateTime.UtcNow && Status != InvoiceStatus.Cancelled;

    public string StatusDisplay => Status switch
    {
        InvoiceStatus.Draft => "Draft",
        InvoiceStatus.Sent => "Sent",
        InvoiceStatus.PartiallyPaid => "Partially Paid",
        InvoiceStatus.Paid => "Paid",
        InvoiceStatus.Overdue => "Overdue",
        InvoiceStatus.Cancelled => "Cancelled",
        InvoiceStatus.Refunded => "Refunded",
        _ => "Unknown"
    };

    public void RecalculateTotals()
    {
        SubTotal = LaborAmount + PartsAmount + OtherAmount - DiscountAmount;
        TaxAmount = SubTotal * (TaxRate / 100);
        Total = SubTotal + TaxAmount;
    }

    public void UpdateStatus()
    {
        if (Status == InvoiceStatus.Cancelled || Status == InvoiceStatus.Refunded)
            return;

        if (IsPaid)
        {
            Status = InvoiceStatus.Paid;
            PaidAt ??= DateTime.UtcNow;
        }
        else if (AmountPaid > 0)
        {
            Status = InvoiceStatus.PartiallyPaid;
        }
        else if (IsOverdue)
        {
            Status = InvoiceStatus.Overdue;
        }
    }

    public static Invoice FromJob(Job job, string invoiceNumber, decimal taxRate = 7.0m)
    {
        var subTotal = job.LaborTotal + job.PartsTotal;
        var taxAmount = subTotal * (taxRate / 100);
        
        return new Invoice
        {
            InvoiceNumber = invoiceNumber,
            JobId = job.Id,
            EstimateId = job.EstimateId,
            CustomerId = job.CustomerId,
            LaborAmount = job.LaborTotal,
            PartsAmount = job.PartsTotal,
            SubTotal = subTotal,
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            Total = subTotal + taxAmount,
            DueDate = DateTime.UtcNow.AddDays(30),
            Status = InvoiceStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static string GenerateInvoiceNumber(int lastNumber)
    {
        return $"INV-{DateTime.UtcNow:yyyy}-{(lastNumber + 1):D4}";
    }
}
