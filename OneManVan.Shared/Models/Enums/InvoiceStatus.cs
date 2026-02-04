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
/// Pricing type for invoices - determines how the invoice is structured.
/// </summary>
public enum InvoicePricingType
{
    /// <summary>
    /// Itemized breakdown of materials and labor (default)
    /// </summary>
    MaterialAndLabor = 0,
    
    /// <summary>
    /// Single flat rate price (no breakdown shown)
    /// </summary>
    FlatRate = 1,
    
    /// <summary>
    /// Time and materials - hourly rate plus materials at cost
    /// </summary>
    TimeAndMaterials = 2
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
    Other = 7,
    DirectDeposit = 8,
    ACH = 9,
    Invoice = 10
}
