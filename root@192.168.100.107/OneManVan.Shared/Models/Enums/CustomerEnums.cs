namespace OneManVan.Shared.Models.Enums;

/// <summary>
/// Type of customer for pricing and service differentiation.
/// </summary>
public enum CustomerType
{
    Residential = 0,
    Commercial = 1,
    PropertyManager = 2,
    Government = 3,
    NonProfit = 4,
    NewConstruction = 5
}

/// <summary>
/// Customer account status.
/// </summary>
public enum CustomerStatus
{
    Active = 0,
    Inactive = 1,
    Lead = 2,
    VIP = 3,
    DoNotService = 4,
    Delinquent = 5,
    Archived = 6
}

/// <summary>
/// Preferred method of contacting customer.
/// </summary>
public enum ContactMethod
{
    Any = 0,
    Phone = 1,
    Email = 2,
    Text = 3
}

/// <summary>
/// Payment terms for invoicing.
/// </summary>
public enum PaymentTerms
{
    COD = 0,           // Cash on delivery
    DueOnReceipt = 1,  // Payment due immediately
    Net15 = 2,         // Due in 15 days
    Net30 = 3,         // Due in 30 days
    Net45 = 4,         // Due in 45 days
    Net60 = 5          // Due in 60 days
}
