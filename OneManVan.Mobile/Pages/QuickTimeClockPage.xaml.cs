using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

public partial class QuickTimeClockPage : ContentPage
{
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;
    private List<JobViewModel> _todaysJobs = new();
    private JobViewModel? _selectedJob;
    private TimeEntry? _activeTimeEntry;
    private IDispatcherTimer? _timer;

    public QuickTimeClockPage(IDbContextFactory<OneManVanDbContext> dbFactory)
    {
        InitializeComponent();
        _dbFactory = dbFactory;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTodaysJobsAsync();
        await CheckActiveTimeEntryAsync();
        StartTimer();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
    }

    private void StartTimer()
    {
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) => UpdateDurationDisplay();
        _timer.Start();
    }

    private void UpdateDurationDisplay()
    {
        if (_activeTimeEntry != null)
        {
            var duration = DateTime.UtcNow - _activeTimeEntry.StartTime;
            DurationLabel.Text = $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
        }
    }

    private async Task LoadTodaysJobsAsync()
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var today = DateTime.Today;
            var jobs = await db.Jobs
                .Include(j => j.Customer)
                .Include(j => j.Workers)
                .Include(j => j.TimeEntries)
                .Where(j => j.ScheduledDate.HasValue && j.ScheduledDate.Value.Date == today)
                .Where(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled)
                .OrderBy(j => j.ArrivalWindowStart)
                .AsNoTracking()
                .ToListAsync();

            _todaysJobs = jobs.Select(j => new JobViewModel
            {
                Id = j.Id,
                Title = j.Title ?? "Untitled Job",
                CustomerName = j.Customer?.DisplayName ?? "Unknown Customer",
                Status = j.Status,
                StatusText = j.Status.ToString(),
                StatusColor = GetStatusColor(j.Status),
                ScheduledTime = j.ArrivalWindowStart.HasValue 
                    ? $"Scheduled: {DateTime.Today.Add(j.ArrivalWindowStart.Value):h:mm tt}"
                    : "No time set",
                HoursWorked = j.TimeEntries?
                    .Where(te => te.EndTime.HasValue)
                    .Sum(te => (te.EndTime!.Value - te.StartTime).TotalHours) ?? 0,
                HasHoursWorked = j.TimeEntries?.Any() ?? false
            }).ToList();

            foreach (var job in _todaysJobs)
            {
                job.HoursWorkedDisplay = $"{job.HoursWorked:N1} hrs worked";
            }

            TodayJobsCollection.ItemsSource = _todaysJobs;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load jobs: {ex.Message}", "OK");
        }
    }

    private async Task CheckActiveTimeEntryAsync()
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            // Find any active time entry for today (not ended)
            // In a real app, you'd filter by the current employee
            _activeTimeEntry = await db.TimeEntries
                .Include(te => te.Job)
                .Where(te => te.EndTime == null)
                .OrderByDescending(te => te.StartTime)
                .FirstOrDefaultAsync();

            if (_activeTimeEntry != null)
            {
                // Currently clocked in
                UpdateUIForClockedIn(_activeTimeEntry);
            }
            else
            {
                UpdateUIForClockedOut();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to check status: {ex.Message}", "OK");
        }
    }

    private void UpdateUIForClockedIn(TimeEntry entry)
    {
        StatusBanner.BackgroundColor = Color.FromArgb("#10b981"); // Success green
        StatusLabel.Text = entry.EntryType == TimeEntryType.Break ? "ON BREAK" : "CLOCKED IN";
        CurrentJobLabel.Text = entry.Job?.Title ?? $"Job #{entry.JobId}";
        DurationLabel.IsVisible = true;
        
        ClockInButton.IsVisible = false;
        ClockOutButton.IsVisible = true;
        BreakButton.IsVisible = entry.EntryType != TimeEntryType.Break;
    }

    private void UpdateUIForClockedOut()
    {
        StatusBanner.BackgroundColor = Color.FromArgb("#6b7280"); // Gray
        StatusLabel.Text = "NOT CLOCKED IN";
        CurrentJobLabel.Text = "Select a job to clock in";
        DurationLabel.IsVisible = false;
        DurationLabel.Text = "00:00:00";
        
        ClockInButton.IsVisible = true;
        ClockInButton.IsEnabled = _selectedJob != null;
        ClockOutButton.IsVisible = false;
        BreakButton.IsVisible = false;
    }

    private void OnJobSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is JobViewModel job)
        {
            _selectedJob = job;
            ClockInButton.IsEnabled = true;
            
            if (_activeTimeEntry == null)
            {
                CurrentJobLabel.Text = $"Ready: {job.Title}";
            }
        }
    }

    private async void OnClockInClicked(object sender, EventArgs e)
    {
        if (_selectedJob == null)
        {
            await DisplayAlert("Select Job", "Please select a job to clock in to.", "OK");
            return;
        }

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            // Check if there's an existing JobWorker for this job
            // For simplicity, we'll create a time entry directly
            // In production, you'd associate with an employee/worker
            
            var timeEntry = new TimeEntry
            {
                JobId = _selectedJob.Id,
                StartTime = DateTime.UtcNow,
                EntryType = TimeEntryType.Labor,
                // EmployeeId would be set from current user context
            };

            db.TimeEntries.Add(timeEntry);
            
            // Update job status to InProgress
            var job = await db.Jobs.FindAsync(_selectedJob.Id);
            if (job != null && job.Status == JobStatus.Scheduled)
            {
                job.Status = JobStatus.InProgress;
                job.StartedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();

            _activeTimeEntry = timeEntry;
            UpdateUIForClockedIn(timeEntry);
            
            // Haptic feedback
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            
            await DisplayAlert("Clocked In", $"You are now clocked in to:\n{_selectedJob.Title}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to clock in: {ex.Message}", "OK");
        }
    }

    private async void OnClockOutClicked(object sender, EventArgs e)
    {
        if (_activeTimeEntry == null) return;

        var confirm = await DisplayAlert("Clock Out", 
            "Are you sure you want to clock out?", "Clock Out", "Cancel");
        
        if (!confirm) return;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var entry = await db.TimeEntries.FindAsync(_activeTimeEntry.Id);
            if (entry != null)
            {
                entry.EndTime = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            _activeTimeEntry = null;
            UpdateUIForClockedOut();
            
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            
            await LoadTodaysJobsAsync(); // Refresh to show updated hours
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to clock out: {ex.Message}", "OK");
        }
    }

    private async void OnBreakClicked(object sender, EventArgs e)
    {
        if (_activeTimeEntry == null) return;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            // End current entry
            var currentEntry = await db.TimeEntries.FindAsync(_activeTimeEntry.Id);
            if (currentEntry != null)
            {
                currentEntry.EndTime = DateTime.UtcNow;
            }

            // Start break entry
            var breakEntry = new TimeEntry
            {
                JobId = _activeTimeEntry.JobId,
                EmployeeId = _activeTimeEntry.EmployeeId,
                StartTime = DateTime.UtcNow,
                EntryType = TimeEntryType.Break
            };

            db.TimeEntries.Add(breakEntry);
            await db.SaveChangesAsync();

            _activeTimeEntry = breakEntry;
            UpdateUIForClockedIn(breakEntry);
            
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to start break: {ex.Message}", "OK");
        }
    }

    private Color GetStatusColor(JobStatus status)
    {
        return status switch
        {
            JobStatus.Scheduled => Color.FromArgb("#10b981"),
            JobStatus.InProgress => Color.FromArgb("#f59e0b"),
            JobStatus.Completed => Color.FromArgb("#06b6d4"),
            JobStatus.Cancelled => Color.FromArgb("#6b7280"),
            _ => Color.FromArgb("#3b82f6")
        };
    }

    // View Model for job display
    public class JobViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public JobStatus Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public Color StatusColor { get; set; } = Colors.Gray;
        public string ScheduledTime { get; set; } = string.Empty;
        public double HoursWorked { get; set; }
        public string HoursWorkedDisplay { get; set; } = string.Empty;
        public bool HasHoursWorked { get; set; }
    }
}
