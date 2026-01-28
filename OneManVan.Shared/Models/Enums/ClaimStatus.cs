namespace OneManVan.Shared.Models.Enums;

/// <summary>
/// Status of a warranty claim.
/// </summary>
public enum ClaimStatus
{
    /// <summary>
    /// Claim has been filed but not yet reviewed.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Manufacturer is reviewing the claim.
    /// </summary>
    UnderReview = 1,

    /// <summary>
    /// Claim has been approved by manufacturer.
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Claim has been denied by manufacturer.
    /// </summary>
    Denied = 3,

    /// <summary>
    /// Repair/replacement completed.
    /// </summary>
    Completed = 4,

    /// <summary>
    /// Claim cancelled by customer or technician.
    /// </summary>
    Cancelled = 5
}
