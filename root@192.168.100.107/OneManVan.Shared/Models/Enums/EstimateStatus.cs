namespace OneManVan.Shared.Models.Enums;

/// <summary>
/// Status of an estimate/proposal.
/// </summary>
public enum EstimateStatus
{
    Draft = 0,
    Sent = 1,
    Accepted = 2,
    Declined = 3,
    Expired = 4,
    Converted = 5  // Converted to job/invoice
}
