namespace OneManVan.Shared.Models;

/// <summary>
/// Represents an alert or notification item for the dashboard.
/// </summary>
public class DashboardAlert
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AlertType Type { get; set; }
    public int Count { get; set; }
    public string ActionUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum AlertType
{
    Info,
    Warning,
    Danger,
    Success
}
