namespace OneManVan.Shared.Models.Enums;

/// <summary>
/// Status of a job in the workflow.
/// </summary>
public enum JobStatus
{
    Draft = 0,
    Scheduled = 1,
    EnRoute = 2,
    InProgress = 3,
    Completed = 4,
    Closed = 5,
    Cancelled = 6,
    OnHold = 7
}
