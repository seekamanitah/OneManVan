namespace OneManVan.Services;

/// <summary>
/// Settings for Google Calendar integration.
/// </summary>
public class GoogleCalendarSettings
{
    public bool IsEnabled { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public bool SyncJobsToCalendar { get; set; } = true;
    public bool AddReminders { get; set; } = true;
    public int ReminderMinutes { get; set; } = 30;
    public string CalendarId { get; set; } = "primary";
    public string? ConnectedEmail { get; set; }
    public string? RefreshToken { get; set; }
    public bool IsAuthorized { get; set; }
    
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);
}
