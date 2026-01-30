namespace OneManVan.Shared.Models.Enums;

/// <summary>
/// Type of service agreement / maintenance contract.
/// </summary>
public enum AgreementType
{
    /// <summary>
    /// Basic plan - 1 visit/year, minimal coverage.
    /// </summary>
    Basic = 0,

    /// <summary>
    /// Standard plan - 2 visits/year, some discounts.
    /// </summary>
    Standard = 1,

    /// <summary>
    /// Premium plan - 2+ visits/year, parts coverage, priority service.
    /// </summary>
    Premium = 2,

    /// <summary>
    /// Annual service agreement (1 year term).
    /// </summary>
    Annual = 3,

    /// <summary>
    /// Semi-annual service agreement (6 month term).
    /// </summary>
    SemiAnnual = 4,

    /// <summary>
    /// Quarterly service agreement (3 month term).
    /// </summary>
    Quarterly = 5,

    /// <summary>
    /// Custom agreement with special terms.
    /// </summary>
    Custom = 6
}

/// <summary>
/// Status of a service agreement.
/// </summary>
public enum AgreementStatus
{
    /// <summary>
    /// Agreement created but not yet activated.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Awaiting customer signature or payment.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Active and in force.
    /// </summary>
    Active = 2,

    /// <summary>
    /// Agreement has expired (end date passed).
    /// </summary>
    Expired = 3,

    /// <summary>
    /// Customer cancelled the agreement.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Temporarily suspended (e.g., payment issue).
    /// </summary>
    Suspended = 5
}

/// <summary>
/// How often the customer is billed for the agreement.
/// </summary>
public enum BillingFrequency
{
    /// <summary>
    /// Single annual payment.
    /// </summary>
    Annual = 0,

    /// <summary>
    /// Two payments per year.
    /// </summary>
    SemiAnnual = 1,

    /// <summary>
    /// Four payments per year.
    /// </summary>
    Quarterly = 2,

    /// <summary>
    /// Monthly payments.
    /// </summary>
    Monthly = 3,

    /// <summary>
    /// Per-visit payment.
    /// </summary>
    PerVisit = 4
}
