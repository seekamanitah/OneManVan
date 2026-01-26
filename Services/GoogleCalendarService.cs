using System.IO;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using OneManVan.Shared.Models;

namespace OneManVan.Services;

/// <summary>
/// Service for syncing jobs with Google Calendar.
/// </summary>
public class GoogleCalendarService
{
    private readonly string _settingsPath;
    private readonly string _tokenPath;
    private GoogleCalendarSettings _settings;
    private CalendarService? _calendarService;

    private static readonly string[] Scopes = { CalendarService.Scope.Calendar };
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public GoogleCalendarService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appData, "OneManVan");
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "google_calendar_settings.json");
        _tokenPath = Path.Combine(appFolder, "google_tokens");
        _settings = LoadSettings();
    }

    /// <summary>
    /// Gets the current Google Calendar settings.
    /// </summary>
    public GoogleCalendarSettings Settings => _settings;

    /// <summary>
    /// Whether Google Calendar is properly configured and authorized.
    /// </summary>
    public bool IsAvailable => _settings.IsEnabled && _settings.IsConfigured && _settings.IsAuthorized;

    /// <summary>
    /// Loads settings from disk.
    /// </summary>
    private GoogleCalendarSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<GoogleCalendarSettings>(json) ?? new GoogleCalendarSettings();
            }
        }
        catch
        {
            // Return default settings on error
        }
        return new GoogleCalendarSettings();
    }

    /// <summary>
    /// Saves settings to disk.
    /// </summary>
    public void SaveSettings(GoogleCalendarSettings settings)
    {
        _settings = settings;
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, json);
        
        // Reset service so it's recreated with new settings
        _calendarService = null;
    }

    /// <summary>
    /// Initiates the OAuth authorization flow.
    /// Returns the authorization URL for the user to visit.
    /// </summary>
    public async Task<(bool Success, string? Email, string? Error)> AuthorizeAsync()
    {
        if (!_settings.IsConfigured)
        {
            return (false, null, "Google Calendar settings are not configured");
        }

        try
        {
            // Create client secrets from settings
            var clientSecrets = new ClientSecrets
            {
                ClientId = _settings.ClientId,
                ClientSecret = _settings.ClientSecret
            };

            // Authorize and get credentials
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(_tokenPath, true));

            // Create the calendar service
            _calendarService = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "OneManVan"
            });

            // Get the user's email from their calendar
            var calendarList = await _calendarService.CalendarList.Get("primary").ExecuteAsync();
            var email = calendarList?.Summary ?? calendarList?.Id ?? "Unknown";

            // Update settings
            _settings.IsAuthorized = true;
            _settings.ConnectedEmail = email;
            _settings.RefreshToken = credential.Token.RefreshToken;
            SaveSettings(_settings);

            return (true, email, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"Authorization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Disconnects the Google account and clears tokens.
    /// </summary>
    public void Disconnect()
    {
        _settings.IsAuthorized = false;
        _settings.ConnectedEmail = null;
        _settings.RefreshToken = null;
        SaveSettings(_settings);
        
        // Clear token cache
        if (Directory.Exists(_tokenPath))
        {
            try
            {
                Directory.Delete(_tokenPath, true);
            }
            catch { }
        }
        
        _calendarService = null;
    }

    /// <summary>
    /// Gets or creates the Calendar service.
    /// </summary>
    private async Task<CalendarService?> GetServiceAsync()
    {
        if (_calendarService != null) return _calendarService;
        
        if (!_settings.IsConfigured || !_settings.IsAuthorized)
        {
            return null;
        }

        try
        {
            var clientSecrets = new ClientSecrets
            {
                ClientId = _settings.ClientId,
                ClientSecret = _settings.ClientSecret
            };

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(_tokenPath, true));

            _calendarService = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "OneManVan"
            });

            return _calendarService;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a calendar event for a job.
    /// </summary>
    public async Task<CalendarSyncResult> CreateJobEventAsync(Job job, Customer customer, Site? site = null)
    {
        if (!IsAvailable || !_settings.SyncJobsToCalendar)
        {
            return new CalendarSyncResult { Success = false, ErrorMessage = "Calendar sync is not enabled" };
        }

        var service = await GetServiceAsync();
        if (service == null)
        {
            return new CalendarSyncResult { Success = false, ErrorMessage = "Could not connect to Google Calendar" };
        }

        try
        {
            var scheduledDate = job.ScheduledDate ?? DateTime.Today;
            var endDate = scheduledDate.AddHours((double)(job.EstimatedHours > 0 ? job.EstimatedHours : 2));
            
            var newEvent = new Event
            {
                Summary = $"Job: {customer.Name}",
                Description = BuildEventDescription(job, customer, site),
                Location = site != null ? $"{site.Address}, {site.City}, {site.State} {site.ZipCode}" : null,
                Start = new EventDateTime
                {
                    DateTimeDateTimeOffset = new DateTimeOffset(scheduledDate, TimeZoneInfo.Local.GetUtcOffset(scheduledDate)),
                    TimeZone = TimeZoneInfo.Local.Id
                },
                End = new EventDateTime
                {
                    DateTimeDateTimeOffset = new DateTimeOffset(endDate, TimeZoneInfo.Local.GetUtcOffset(endDate)),
                    TimeZone = TimeZoneInfo.Local.Id
                },
                ColorId = GetColorForJobStatus(job.Status)
            };

            // Add reminders if enabled
            if (_settings.AddReminders)
            {
                newEvent.Reminders = new Event.RemindersData
                {
                    UseDefault = false,
                    Overrides = [new EventReminder { Method = "popup", Minutes = _settings.ReminderMinutes }]
                };
            }

            var request = service.Events.Insert(newEvent, _settings.CalendarId);
            var createdEvent = await request.ExecuteAsync();

            return new CalendarSyncResult
            {
                Success = true,
                EventId = createdEvent.Id,
                EventLink = createdEvent.HtmlLink
            };
        }
        catch (Exception ex)
        {
            return new CalendarSyncResult
            {
                Success = false,
                ErrorMessage = $"Failed to create event: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Updates an existing calendar event for a job.
    /// </summary>
    public async Task<CalendarSyncResult> UpdateJobEventAsync(string eventId, Job job, Customer customer, Site? site = null)
    {
        if (!IsAvailable)
        {
            return new CalendarSyncResult { Success = false, ErrorMessage = "Calendar sync is not enabled" };
        }

        var service = await GetServiceAsync();
        if (service == null)
        {
            return new CalendarSyncResult { Success = false, ErrorMessage = "Could not connect to Google Calendar" };
        }

        try
        {
            // Get existing event
            var existingEvent = await service.Events.Get(_settings.CalendarId, eventId).ExecuteAsync();
            
            var scheduledDate = job.ScheduledDate ?? DateTime.Today;
            var endDate = scheduledDate.AddHours((double)(job.EstimatedHours > 0 ? job.EstimatedHours : 2));
            
            existingEvent.Summary = $"Job: {customer.Name}";
            existingEvent.Description = BuildEventDescription(job, customer, site);
            existingEvent.Location = site != null ? $"{site.Address}, {site.City}, {site.State} {site.ZipCode}" : null;
            existingEvent.Start = new EventDateTime
            {
                DateTimeDateTimeOffset = new DateTimeOffset(scheduledDate, TimeZoneInfo.Local.GetUtcOffset(scheduledDate)),
                TimeZone = TimeZoneInfo.Local.Id
            };
            existingEvent.End = new EventDateTime
            {
                DateTimeDateTimeOffset = new DateTimeOffset(endDate, TimeZoneInfo.Local.GetUtcOffset(endDate)),
                TimeZone = TimeZoneInfo.Local.Id
            };
            existingEvent.ColorId = GetColorForJobStatus(job.Status);

            var request = service.Events.Update(existingEvent, _settings.CalendarId, eventId);
            var updatedEvent = await request.ExecuteAsync();

            return new CalendarSyncResult
            {
                Success = true,
                EventId = updatedEvent.Id,
                EventLink = updatedEvent.HtmlLink
            };
        }
        catch (Exception ex)
        {
            return new CalendarSyncResult
            {
                Success = false,
                ErrorMessage = $"Failed to update event: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Deletes a calendar event.
    /// </summary>
    public async Task<CalendarSyncResult> DeleteEventAsync(string eventId)
    {
        if (!IsAvailable)
        {
            return new CalendarSyncResult { Success = false, ErrorMessage = "Calendar sync is not enabled" };
        }

        var service = await GetServiceAsync();
        if (service == null)
        {
            return new CalendarSyncResult { Success = false, ErrorMessage = "Could not connect to Google Calendar" };
        }

        try
        {
            await service.Events.Delete(_settings.CalendarId, eventId).ExecuteAsync();
            return new CalendarSyncResult { Success = true };
        }
        catch (Exception ex)
        {
            return new CalendarSyncResult
            {
                Success = false,
                ErrorMessage = $"Failed to delete event: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Gets upcoming events from the calendar.
    /// </summary>
    public async Task<List<CalendarEventInfo>> GetUpcomingEventsAsync(int maxResults = 10)
    {
        if (!IsAvailable)
        {
            return [];
        }

        var service = await GetServiceAsync();
        if (service == null)
        {
            return [];
        }

        try
        {
            var request = service.Events.List(_settings.CalendarId);
            request.TimeMinDateTimeOffset = DateTimeOffset.Now;
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = maxResults;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            var events = await request.ExecuteAsync();
            
            return events.Items?.Select(e => new CalendarEventInfo
            {
                Id = e.Id,
                Title = e.Summary,
                Start = e.Start.DateTimeDateTimeOffset?.DateTime ?? DateTime.Parse(e.Start.Date ?? DateTime.Today.ToString()),
                End = e.End.DateTimeDateTimeOffset?.DateTime ?? DateTime.Parse(e.End.Date ?? DateTime.Today.ToString()),
                Location = e.Location,
                Link = e.HtmlLink
            }).ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Tests the connection to Google Calendar.
    /// </summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync()
    {
        if (!_settings.IsConfigured)
        {
            return (false, "Google Calendar is not configured");
        }

        if (!_settings.IsAuthorized)
        {
            return (false, "Not authorized. Click 'Connect' to authorize.");
        }

        var service = await GetServiceAsync();
        if (service == null)
        {
            return (false, "Could not create calendar service");
        }

        try
        {
            var calendar = await service.CalendarList.Get(_settings.CalendarId).ExecuteAsync();
            return (true, $"Connected to: {calendar.Summary ?? _settings.CalendarId}");
        }
        catch (Exception ex)
        {
            return (false, $"Connection failed: {ex.Message}");
        }
    }

    private static string BuildEventDescription(Job job, Customer customer, Site? site)
    {
        var lines = new List<string>
        {
            $"Customer: {customer.Name}",
            $"Phone: {customer.Phone ?? "N/A"}",
            $"Email: {customer.Email ?? "N/A"}"
        };

        if (site != null)
        {
            lines.Add($"Site: {site.Address}");
        }

        if (!string.IsNullOrEmpty(job.Notes))
        {
            lines.Add($"\nNotes:\n{job.Notes}");
        }

        lines.Add($"\n---\nManaged by OneManVan");

        return string.Join("\n", lines);
    }

    private static string GetColorForJobStatus(Shared.Models.Enums.JobStatus status)
    {
        // Google Calendar color IDs
        return status switch
        {
            Shared.Models.Enums.JobStatus.Draft => "8",      // Gray
            Shared.Models.Enums.JobStatus.Scheduled => "1",  // Lavender
            Shared.Models.Enums.JobStatus.EnRoute => "5",    // Yellow
            Shared.Models.Enums.JobStatus.InProgress => "6", // Orange
            Shared.Models.Enums.JobStatus.Completed => "10", // Green
            Shared.Models.Enums.JobStatus.Closed => "2",     // Sage
            Shared.Models.Enums.JobStatus.Cancelled => "11", // Red
            Shared.Models.Enums.JobStatus.OnHold => "3",     // Grape
            _ => "1"
        };
    }
}

/// <summary>
/// Result of a calendar sync operation.
/// </summary>
public class CalendarSyncResult
{
    public bool Success { get; set; }
    public string? EventId { get; set; }
    public string? EventLink { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Information about a calendar event.
/// </summary>
public class CalendarEventInfo
{
    public string Id { get; set; } = string.Empty;
    public string? Title { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string? Location { get; set; }
    public string? Link { get; set; }
}
