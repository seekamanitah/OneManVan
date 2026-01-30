using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Payment transaction for an invoice.
/// </summary>
public class Payment
{
    public int Id { get; set; }

    [Required]
    public int InvoiceId { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? TransactionId { get; set; }

    [MaxLength(100)]
    public string? ReferenceNumber { get; set; }

    [MaxLength(4)]
    public string? CardLast4 { get; set; }

    public bool IsRefund { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal ProcessingFee { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal NetAmount { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Invoice Invoice { get; set; } = null!;

    public string MethodDisplay => Method switch
    {
        PaymentMethod.Cash => "Cash",
        PaymentMethod.Check => "Check",
        PaymentMethod.CreditCard => "Credit Card",
        PaymentMethod.DebitCard => "Debit Card",
        PaymentMethod.BankTransfer => "Bank Transfer",
        PaymentMethod.Digital => "Digital Payment",
        PaymentMethod.Financing => "Financing",
        PaymentMethod.Other => "Other",
        _ => "Unknown"
    };

    public string Description
    {
        get
        {
            var desc = MethodDisplay;
            if (!string.IsNullOrEmpty(CardLast4))
                desc += $" ****{CardLast4}";
            if (!string.IsNullOrEmpty(ReferenceNumber))
                desc += $" #{ReferenceNumber}";
            return desc;
        }
    }

    public void CalculateNetAmount()
    {
        NetAmount = Amount - ProcessingFee;
    }

    public static Payment CreateRefund(int invoiceId, decimal amount, PaymentMethod method, string? transactionId = null)
    {
        return new Payment
        {
            InvoiceId = invoiceId,
            Amount = -Math.Abs(amount),
            Method = method,
            TransactionId = transactionId,
            IsRefund = true,
            PaymentDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
}
