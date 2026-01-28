using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using OneManVan.Services;

namespace OneManVan.Dialogs;

/// <summary>
/// Read-only dialog for viewing job details with option to edit.
/// </summary>
public partial class JobDetailDialog : Window
{
    private readonly OneManVanDbContext _dbContext;
    private readonly Job _job;
    public bool JobWasEdited { get; private set; }

    public JobDetailDialog(OneManVanDbContext dbContext, Job job)
    {
        InitializeComponent();
        _dbContext = dbContext;
        _job = job;

        LoadJobDetailsAsync();
    }

    private async void LoadJobDetailsAsync()
    {
        try
        {
            // Reload job with all relationships
            var job = await _dbContext.Jobs
                .Include(j => j.Customer)
                .Include(j => j.Site)
                .Include(j => j.Asset)
                .FirstOrDefaultAsync(j => j.Id == _job.Id);

            if (job == null)
            {
                ToastService.Error("Job not found");
                Close();
                return;
            }

            // Header
            JobTitleText.Text = job.Title ?? "Untitled Job";
            JobNumberText.Text = job.JobNumber ?? $"Job #{job.Id}";

            // Status Badge
            SetStatusBadge(job.Status);
            SetPriorityBadge(job.Priority);

            // Customer Information
            if (job.Customer != null)
            {
                CustomerNameText.Text = job.Customer.Name;
                CustomerPhoneText.Text = job.Customer.Phone ?? "No phone";
            }
            else
            {
                CustomerNameText.Text = "No customer";
                CustomerPhoneText.Text = "";
            }

            // Site
            if (job.Site != null)
            {
                SiteText.Text = job.Site.FullAddress;
            }
            else
            {
                SiteText.Text = "No site specified";
                SiteText.Foreground = (Brush)FindResource("SubtextBrush");
            }

            // Asset
            if (job.Asset != null)
            {
                var assetDisplay = $"{job.Asset.Brand} {job.Asset.Model}";
                if (!string.IsNullOrEmpty(job.Asset.Nickname))
                    assetDisplay = $"{job.Asset.Nickname} - {assetDisplay}";
                AssetText.Text = assetDisplay;
            }
            else
            {
                AssetText.Text = "No equipment specified";
                AssetText.Foreground = (Brush)FindResource("SubtextBrush");
            }

            // Job Details
            DescriptionText.Text = string.IsNullOrWhiteSpace(job.Description) 
                ? "No description provided" 
                : job.Description;
            
            if (string.IsNullOrWhiteSpace(job.Description))
            {
                DescriptionText.Foreground = (Brush)FindResource("SubtextBrush");
            }

            JobTypeText.Text = GetJobTypeDisplay(job.JobType);
            CreatedAtText.Text = job.CreatedAt.ToString("MMM d, yyyy h:mm tt");

            // Scheduling
            if (job.ScheduledDate.HasValue)
            {
                ScheduledDateText.Text = job.ScheduledDate.Value.ToString("dddd, MMM d, yyyy");
            }
            else
            {
                ScheduledDateText.Text = "Not scheduled";
                ScheduledDateText.Foreground = (Brush)FindResource("SubtextBrush");
            }

            if (job.ArrivalWindowStart.HasValue && job.ArrivalWindowEnd.HasValue)
            {
                TimeWindowText.Text = $"{job.ArrivalWindowStart.Value:h\\:mm} - {job.ArrivalWindowEnd.Value:h\\:mm}";
            }
            else if (job.ArrivalWindowStart.HasValue)
            {
                TimeWindowText.Text = $"Starting at {job.ArrivalWindowStart.Value:h\\:mm}";
            }
            else
            {
                TimeWindowText.Text = "No time window";
                TimeWindowText.Foreground = (Brush)FindResource("SubtextBrush");
            }

            if (job.EstimatedHours > 0)
            {
                var hours = (int)job.EstimatedHours;
                var minutes = (int)((job.EstimatedHours - hours) * 60);
                
                if (hours > 0 && minutes > 0)
                    DurationText.Text = $"{hours} hour{(hours > 1 ? "s" : "")} {minutes} min";
                else if (hours > 0)
                    DurationText.Text = $"{hours} hour{(hours > 1 ? "s" : "")}";
                else
                    DurationText.Text = $"{minutes} minutes";
            }
            else
            {
                DurationText.Text = "Not specified";
                DurationText.Foreground = (Brush)FindResource("SubtextBrush");
            }

            // Timeline
            BuildTimeline(job);

            // Additional Fields (show only if populated)
            if (!string.IsNullOrWhiteSpace(job.ProblemDescription))
            {
                ProblemDescriptionLabel.Visibility = Visibility.Visible;
                ProblemDescriptionBorder.Visibility = Visibility.Visible;
                ProblemDescriptionText.Text = job.ProblemDescription;
            }

            if (!string.IsNullOrWhiteSpace(job.WorkPerformed))
            {
                WorkPerformedLabel.Visibility = Visibility.Visible;
                WorkPerformedBorder.Visibility = Visibility.Visible;
                WorkPerformedText.Text = job.WorkPerformed;
            }

            if (!string.IsNullOrWhiteSpace(job.InternalNotes))
            {
                NotesLabel.Visibility = Visibility.Visible;
                NotesBorder.Visibility = Visibility.Visible;
                NotesText.Text = job.InternalNotes;
            }
        }
        catch (Exception ex)
        {
            ToastService.Error($"Failed to load job details: {ex.Message}");
        }
    }

    private void SetStatusBadge(JobStatus status)
    {
        StatusText.Text = status.ToString();
        
        var color = status switch
        {
            JobStatus.Draft => Color.FromRgb(158, 158, 158),
            JobStatus.Scheduled => Color.FromRgb(33, 150, 243),
            JobStatus.EnRoute => Color.FromRgb(255, 152, 0),
            JobStatus.InProgress => Color.FromRgb(76, 175, 80),
            JobStatus.Completed => Color.FromRgb(103, 58, 183),
            JobStatus.OnHold => Color.FromRgb(244, 67, 54),
            JobStatus.Cancelled => Color.FromRgb(158, 158, 158),
            JobStatus.Closed => Color.FromRgb(96, 125, 139),
            _ => Color.FromRgb(158, 158, 158)
        };

        StatusBadge.Background = new SolidColorBrush(color);
    }

    private void SetPriorityBadge(JobPriority priority)
    {
        var (text, color) = priority switch
        {
            JobPriority.Low => ("Low", Color.FromRgb(76, 175, 80)),
            JobPriority.Normal => ("Normal", Color.FromRgb(33, 150, 243)),
            JobPriority.High => ("High", Color.FromRgb(255, 152, 0)),
            JobPriority.Urgent => ("Urgent", Color.FromRgb(244, 67, 54)),
            JobPriority.Emergency => ("Emergency", Color.FromRgb(156, 39, 176)),
            _ => ("Normal", Color.FromRgb(33, 150, 243))
        };

        PriorityText.Text = text;
        PriorityBadge.Background = new SolidColorBrush(color);
    }

    private void BuildTimeline(Job job)
    {
        TimelinePanel.Children.Clear();

        var events = new List<(string Label, DateTime? Timestamp)>
        {
            ("Created", job.CreatedAt),
            ("Scheduled", job.ScheduledDate),
            ("Dispatched", job.DispatchedAt),
            ("En Route", job.EnRouteAt),
            ("Arrived", job.ArrivedAt),
            ("Started Work", job.StartedAt),
            ("Completed", job.CompletedAt),
            ("Closed", job.ClosedAt)
        };

        var hasAnyEvent = events.Any(e => e.Timestamp.HasValue);
        
        if (!hasAnyEvent)
        {
            var emptyText = new TextBlock
            {
                Text = "No timeline events yet",
                Foreground = (Brush)FindResource("SubtextBrush"),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 16)
            };
            TimelinePanel.Children.Add(emptyText);
            return;
        }

        foreach (var (label, timestamp) in events.Where(e => e.Timestamp.HasValue))
        {
            var item = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8)
            };

            // Dot
            var dot = new Border
            {
                Width = 8,
                Height = 8,
                CornerRadius = new CornerRadius(4),
                Background = (Brush)FindResource("PrimaryBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            item.Children.Add(dot);

            // Label and timestamp
            var content = new StackPanel();
            
            var labelText = new TextBlock
            {
                Text = label,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("TextBrush")
            };
            content.Children.Add(labelText);

            var timestampText = new TextBlock
            {
                Text = timestamp.HasValue ? timestamp.Value.ToString("MMM d, yyyy h:mm tt") : "No date",
                FontSize = 11,
                Foreground = (Brush)FindResource("SubtextBrush")
            };
            content.Children.Add(timestampText);

            item.Children.Add(content);
            TimelinePanel.Children.Add(item);
        }
    }

    private static string GetJobTypeDisplay(JobType type) => type switch
    {
        JobType.ServiceCall => "Service Call",
        JobType.StartUp => "Start-Up",
        _ => type.ToString()
    };

    private void OnEditClick(object sender, RoutedEventArgs e)
    {
        var editDialog = new AddEditJobDialog(_dbContext, _job);
        if (editDialog.ShowDialog() == true)
        {
            JobWasEdited = true;
            LoadJobDetailsAsync(); // Refresh data
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
