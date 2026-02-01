using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using System.Text.Json;

namespace OneManVan.Shared.Services;

/// <summary>
/// Service for Google Calendar integration.
/// Manages synchronization between jobs and Google Calendar events.
/// </summary>
public class GoogleCalendarService
{
    private readonly IDbContextFactory<OneManVanDbContext> _contextFactory;
    
    // Settings stored in CompanySettings or app config
    private const string SETTINGS_KEY = "GoogleCalendarSettings";

    public GoogleCalendarService(IDbContextFactory<OneManVanDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Gets the current Google Calendar settings.
    /// </summary>
    public async Task<GoogleCalendarSettings> GetSettingsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var company = await context.CompanySettings.FirstOrDefaultAsync();
        if (company?.GoogleCalendarSettings != null)
        {
            try
            {
                return JsonSerializer.Deserialize<GoogleCalendarSettings>(company.GoogleCalendarSettings) 
                    ?? new GoogleCalendarSettings();
            }
            catch
            {
                return new GoogleCalendarSettings();
            }
        }
        
        return new GoogleCalendarSettings();
    }

    /// <summary>
    /// Saves Google Calendar settings.
    /// </summary>
    public async Task SaveSettingsAsync(GoogleCalendarSettings settings)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var company = await context.CompanySettings.FirstOrDefaultAsync();
        if (company == null)
        {
            company = new CompanySettings();
            context.CompanySettings.Add(company);
        }
        
        company.GoogleCalendarSettings = JsonSerializer.Serialize(settings);
        company.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Generates a Google Calendar event URL for adding a job.
    /// This uses the Google Calendar "Add Event" URL scheme.
    /// </summary>
    public string GenerateAddEventUrl(Job job)
    {
        if (!job.ScheduledDate.HasValue) return string.Empty;

        var startTime = job.ScheduledDate.Value;
        var endTime = job.ScheduledEndDate ?? startTime.AddHours((double)job.EstimatedHours);
        
        var title = Uri.EscapeDataString($"{job.Title} - {job.Customer?.DisplayName ?? "Customer"}");
        var location = Uri.EscapeDataString(GetJobLocation(job));
        var details = Uri.EscapeDataString(GetJobDescription(job));
        
        // Format: YYYYMMDDTHHmmSSZ
        var start = startTime.ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
        var end = endTime.ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
        
        return $"https://calendar.google.com/calendar/render?action=TEMPLATE&text={title}&dates={start}/{end}&location={location}&details={details}";
    }

    /// <summary>
    /// Generates an ICS file content for a job.
    /// </summary>
    public string GenerateIcsContent(Job job)
    {
        if (!job.ScheduledDate.HasValue) return string.Empty;

        var startTime = job.ScheduledDate.Value;
        var endTime = job.ScheduledEndDate ?? startTime.AddHours((double)job.EstimatedHours);
        var uid = $"job-{job.Id}@onemanvan.app";
        
        var ics = $@"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//OneManVan//Job Scheduler//EN
CALSCALE:GREGORIAN
METHOD:PUBLISH
BEGIN:VEVENT
UID:{uid}
DTSTART:{startTime.ToUniversalTime():yyyyMMddTHHmmssZ}
DTEND:{endTime.ToUniversalTime():yyyyMMddTHHmmssZ}
DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}
SUMMARY:{EscapeIcs(job.Title)} - {EscapeIcs(job.Customer?.DisplayName ?? "Customer")}
DESCRIPTION:{EscapeIcs(GetJobDescription(job))}
LOCATION:{EscapeIcs(GetJobLocation(job))}
STATUS:CONFIRMED
PRIORITY:{GetIcsPriority(job.Priority)}
END:VEVENT
END:VCALENDAR";

        return ics;
    }

    /// <summary>
    /// Gets all jobs that need to be synced to calendar.
    /// </summary>
    public async Task<List<Job>> GetJobsForSyncAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var query = context.Jobs
            .Include(j => j.Customer)
            .Include(j => j.Site)
            .Where(j => j.ScheduledDate.HasValue)
            .Where(j => j.Status != JobStatus.Cancelled);

        if (fromDate.HasValue)
            query = query.Where(j => j.ScheduledDate >= fromDate);
        
        if (toDate.HasValue)
            query = query.Where(j => j.ScheduledDate <= toDate);

        return await query
            .OrderBy(j => j.ScheduledDate)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Marks a job as synced with a Google Calendar event ID.
    /// </summary>
    public async Task MarkJobSyncedAsync(int jobId, string googleEventId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var job = await context.Jobs.FindAsync(jobId);
        if (job != null)
        {
            job.GoogleCalendarEventId = googleEventId;
            job.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Gets jobs that haven't been synced to Google Calendar yet.
    /// </summary>
    public async Task<List<Job>> GetUnsyncedJobsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.Jobs
            .Include(j => j.Customer)
            .Include(j => j.Site)
            .Where(j => j.ScheduledDate.HasValue)
            .Where(j => j.Status != JobStatus.Cancelled)
            .Where(j => string.IsNullOrEmpty(j.GoogleCalendarEventId))
            .Where(j => j.ScheduledDate >= DateTime.Today)
            .OrderBy(j => j.ScheduledDate)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Generates a batch ICS file for multiple jobs.
    /// </summary>
    public string GenerateBatchIcsContent(List<Job> jobs)
    {
        var events = string.Join("\n", jobs
            .Where(j => j.ScheduledDate.HasValue)
            .Select(GenerateVEvent));

        return $@"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//OneManVan//Job Scheduler//EN
CALSCALE:GREGORIAN
METHOD:PUBLISH
{events}
END:VCALENDAR";
    }

    private string GenerateVEvent(Job job)
    {
        var startTime = job.ScheduledDate!.Value;
        var endTime = job.ScheduledEndDate ?? startTime.AddHours((double)job.EstimatedHours);
        var uid = $"job-{job.Id}@onemanvan.app";

        return $@"BEGIN:VEVENT
UID:{uid}
DTSTART:{startTime.ToUniversalTime():yyyyMMddTHHmmssZ}
DTEND:{endTime.ToUniversalTime():yyyyMMddTHHmmssZ}
DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}
SUMMARY:{EscapeIcs(job.Title)} - {EscapeIcs(job.Customer?.DisplayName ?? "Customer")}
DESCRIPTION:{EscapeIcs(GetJobDescription(job))}
LOCATION:{EscapeIcs(GetJobLocation(job))}
STATUS:CONFIRMED
PRIORITY:{GetIcsPriority(job.Priority)}
END:VEVENT";
    }

    private string GetJobLocation(Job job)
    {
        if (job.Site != null)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(job.Site.Address)) parts.Add(job.Site.Address);
            if (!string.IsNullOrEmpty(job.Site.City)) parts.Add(job.Site.City);
            if (!string.IsNullOrEmpty(job.Site.State)) parts.Add(job.Site.State);
            if (!string.IsNullOrEmpty(job.Site.ZipCode)) parts.Add(job.Site.ZipCode);
            return string.Join(", ", parts);
        }
        
        return job.Customer?.HomeAddress ?? "";
    }

    private string GetJobDescription(Job job)
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrEmpty(job.Description))
            parts.Add(job.Description);
        
        parts.Add($"Job #: {job.JobNumber ?? job.Id.ToString()}");
        parts.Add($"Customer: {job.Customer?.DisplayName ?? "N/A"}");
        parts.Add($"Priority: {job.Priority}");
        parts.Add($"Type: {job.JobType}");
        
        if (!string.IsNullOrEmpty(job.Customer?.Phone))
            parts.Add($"Phone: {job.Customer.Phone}");
        
        return string.Join("\n", parts);
    }

    private string EscapeIcs(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text
            .Replace("\\", "\\\\")
            .Replace(",", "\\,")
            .Replace(";", "\\;")
            .Replace("\n", "\\n")
            .Replace("\r", "");
    }

    private int GetIcsPriority(JobPriority priority)
    {
        return priority switch
        {
            JobPriority.Emergency => 1,
            JobPriority.High => 3,
            JobPriority.Normal => 5,
            JobPriority.Low => 9,
            _ => 5
        };
    }
}

#region Settings Models

public class GoogleCalendarSettings
{
    public bool IsEnabled { get; set; } = false;
    public string? CalendarId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? RefreshToken { get; set; }
    public string? AccessToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public bool AutoSync { get; set; } = false;
    public int SyncIntervalMinutes { get; set; } = 30;
    public DateTime? LastSyncAt { get; set; }
    
    /// <summary>
    /// Which job statuses to sync.
    /// </summary>
    public List<JobStatus> SyncStatuses { get; set; } = new()
    {
        JobStatus.Scheduled,
        JobStatus.InProgress
    };
    
    /// <summary>
    /// How many days ahead to sync.
    /// </summary>
    public int SyncDaysAhead { get; set; } = 30;
    
    public bool IsConfigured => !string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(ClientSecret);
}

#endregion
