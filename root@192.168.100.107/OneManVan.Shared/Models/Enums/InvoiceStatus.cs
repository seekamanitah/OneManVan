namespace OneManVan.Shared.Models.Enums;

/// <summary>
/// Status of an invoice in the billing workflow.
/// </summary>
public enum InvoiceStatus
{
    Draft = 0,
    Sent = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Overdue = 4,
    Cancelled = 5,
    Refunded = 6
}

/// <summary>
/// Payment method for transactions.
/// </summary>
public enum PaymentMethod
{
    Cash = 0,
    Check = 1,
    CreditCard = 2,
    DebitCard = 3,
    BankTransfer = 4,
    Digital = 5,
    Financing = 6,
    Other = 7
}
