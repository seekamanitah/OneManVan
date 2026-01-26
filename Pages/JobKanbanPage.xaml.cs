using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using OneManVan.Services;

namespace OneManVan.Pages;

/// <summary>
/// Kanban board view for job management with drag-and-drop status updates.
/// </summary>
public partial class JobKanbanPage : UserControl
{
    private readonly OneManVanDbContext _dbContext;
    private List<Job> _allJobs = [];
    private Job? _draggedJob;

    public JobKanbanPage(OneManVanDbContext dbContext)
    {
        InitializeComponent();
        _dbContext = dbContext;
        Loaded += OnPageLoaded;
    }

    // Simple relay command for double-click
    private class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute) => _execute = execute;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        await LoadJobsAsync();
    }

    private async Task LoadJobsAsync()
    {
        try
        {
            LoadingOverlay.Visibility = Visibility.Visible;

            _allJobs = await _dbContext.Jobs
                .Include(j => j.Customer)
                .Include(j => j.Site)
                .Include(j => j.Asset)
                .Where(j => j.Status != JobStatus.Closed && j.Status != JobStatus.Cancelled)
                .OrderByDescending(j => j.Priority)
                .ThenBy(j => j.ScheduledDate)
                .ToListAsync();

            ApplyFiltersAndRefresh();
        }
        catch (Exception ex)
        {
            ToastService.Error($"Failed to load jobs: {ex.Message}");
        }
        finally
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void ApplyFiltersAndRefresh()
    {
        var filtered = _allJobs.AsEnumerable();

        // Date filter
        var dateFilter = (DateFilterCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Dates";
        filtered = dateFilter switch
        {
            "Today" => filtered.Where(j => j.ScheduledDate?.Date == DateTime.Today),
            "This Week" => filtered.Where(j => j.ScheduledDate >= DateTime.Today && 
                                                j.ScheduledDate < DateTime.Today.AddDays(7)),
            "This Month" => filtered.Where(j => j.ScheduledDate?.Month == DateTime.Today.Month && 
                                                 j.ScheduledDate?.Year == DateTime.Today.Year),
            "Overdue" => filtered.Where(j => j.ScheduledDate < DateTime.Today && 
                                              j.Status != JobStatus.Completed),
            _ => filtered
        };

        // Priority filter
        var priorityFilter = (PriorityFilterCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
        if (priorityFilter != "All")
        {
            var priority = priorityFilter switch
            {
                "?? Low" => JobPriority.Low,
                "?? Normal" => JobPriority.Normal,
                "?? High" => JobPriority.High,
                "?? Urgent" => JobPriority.Urgent,
                "?? Emergency" => JobPriority.Emergency,
                _ => (JobPriority?)null
            };
            if (priority.HasValue)
            {
                filtered = filtered.Where(j => j.Priority == priority.Value);
            }
        }

        // Search filter
        var searchText = SearchBox.Text?.Trim().ToLower() ?? "";
        if (!string.IsNullOrEmpty(searchText))
        {
            filtered = filtered.Where(j =>
                (j.Title?.ToLower().Contains(searchText) ?? false) ||
                (j.JobNumber?.ToLower().Contains(searchText) ?? false) ||
                (j.Customer?.Name?.ToLower().Contains(searchText) ?? false) ||
                (j.Description?.ToLower().Contains(searchText) ?? false));
        }

        var filteredList = filtered.ToList();

        // Populate columns
        PopulateColumn(DraftColumn, filteredList.Where(j => j.Status == JobStatus.Draft).ToList(), DraftCount);
        PopulateColumn(ScheduledColumn, filteredList.Where(j => j.Status == JobStatus.Scheduled).ToList(), ScheduledCount);
        PopulateColumn(EnRouteColumn, filteredList.Where(j => j.Status == JobStatus.EnRoute).ToList(), EnRouteCount);
        PopulateColumn(InProgressColumn, filteredList.Where(j => j.Status == JobStatus.InProgress).ToList(), InProgressCount);
        PopulateColumn(CompletedColumn, filteredList.Where(j => j.Status == JobStatus.Completed).ToList(), CompletedCount);
        PopulateColumn(OnHoldColumn, filteredList.Where(j => j.Status == JobStatus.OnHold).ToList(), OnHoldCount);

        // Update subtitle
        var totalCount = filteredList.Count;
        SubtitleText.Text = totalCount == _allJobs.Count
            ? "Drag and drop jobs between columns to update status"
            : $"Showing {totalCount} of {_allJobs.Count} jobs";
    }

    private void PopulateColumn(ItemsControl column, List<Job> jobs, TextBlock countLabel)
    {
        column.Items.Clear();
        countLabel.Text = jobs.Count.ToString();

        foreach (var job in jobs)
        {
            var card = CreateJobCard(job);
            column.Items.Add(card);
        }

        // Add empty state if no jobs
        if (!jobs.Any())
        {
            var emptyState = new TextBlock
            {
                Text = "No jobs",
                Foreground = (Brush)FindResource("SubtextBrush"),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };
            column.Items.Add(emptyState);
        }
    }

    private Border CreateJobCard(Job job)
    {
        var card = new Border
        {
            Background = (Brush)FindResource("BackgroundBrush"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 8),
            Cursor = Cursors.Hand,
            BorderThickness = new Thickness(1),
            BorderBrush = (Brush)FindResource("Surface2Brush"),
            Tag = job
        };

        // Enable drag and double-click
        card.MouseLeftButtonDown += OnJobCardMouseDown;
        card.MouseMove += OnJobCardMouseMove;
        
        // Add double-click via InputBinding
        card.InputBindings.Add(new MouseBinding(
            new RelayCommand(() => OnJobCardDoubleClickCommand(job)),
            new MouseGesture(MouseAction.LeftDoubleClick)));

        var content = new StackPanel();

        // Header with job number and priority
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var jobNumber = new TextBlock
        {
            Text = job.JobNumber ?? $"Job #{job.Id}",
            FontSize = 11,
            Foreground = (Brush)FindResource("SubtextBrush")
        };
        Grid.SetColumn(jobNumber, 0);
        header.Children.Add(jobNumber);

        var priorityBadge = CreatePriorityBadge(job.Priority);
        Grid.SetColumn(priorityBadge, 1);
        header.Children.Add(priorityBadge);

        content.Children.Add(header);

        // Title
        var title = new TextBlock
        {
            Text = job.Title ?? "Untitled Job",
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("TextBrush"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 4)
        };
        content.Children.Add(title);

        // Customer
        if (job.Customer != null)
        {
            var customer = new TextBlock
            {
                Text = $"?? {job.Customer.Name}",
                FontSize = 12,
                Foreground = (Brush)FindResource("SubtextBrush"),
                Margin = new Thickness(0, 2, 0, 0)
            };
            content.Children.Add(customer);
        }

        // Schedule date
        if (job.ScheduledDate.HasValue)
        {
            var isOverdue = job.ScheduledDate.Value.Date < DateTime.Today && job.Status != JobStatus.Completed;
            var scheduleDate = new TextBlock
            {
                Text = $"?? {job.ScheduledDate.Value:MMM d, yyyy}",
                FontSize = 12,
                Foreground = isOverdue 
                    ? new SolidColorBrush(Color.FromRgb(244, 67, 54)) 
                    : (Brush)FindResource("SubtextBrush"),
                FontWeight = isOverdue ? FontWeights.SemiBold : FontWeights.Normal,
                Margin = new Thickness(0, 2, 0, 0)
            };
            content.Children.Add(scheduleDate);
        }

        // Job type badge
        var typeBadge = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(30, 33, 150, 243)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2),
            Margin = new Thickness(0, 8, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        typeBadge.Child = new TextBlock
        {
            Text = GetJobTypeDisplay(job.JobType),
            FontSize = 10,
            Foreground = (Brush)FindResource("PrimaryBrush")
        };
        content.Children.Add(typeBadge);

        card.Child = content;
        return card;
    }

    private Border CreatePriorityBadge(JobPriority priority)
    {
        var (color, emoji) = priority switch
        {
            JobPriority.Low => (Color.FromRgb(76, 175, 80), ""),
            JobPriority.Normal => (Color.FromRgb(33, 150, 243), ""),
            JobPriority.High => (Color.FromRgb(255, 152, 0), ""),
            JobPriority.Urgent => (Color.FromRgb(244, 67, 54), ""),
            JobPriority.Emergency => (Color.FromRgb(156, 39, 176), ""),
            _ => (Color.FromRgb(158, 158, 158), "?")
        };

        return new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(30, color.R, color.G, color.B)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(4, 2, 4, 2),
            Child = new TextBlock
            {
                Text = emoji,
                FontSize = 10
            }
        };
    }

    private static string GetJobTypeDisplay(JobType type) => type switch
    {
        JobType.ServiceCall => "Service Call",
        JobType.Maintenance => "Maintenance",
        JobType.Installation => "Installation",
        JobType.Repair => "Repair",
        JobType.Warranty => "Warranty",
        JobType.Inspection => "Inspection",
        JobType.Emergency => "Emergency",
        JobType.Estimate => "Estimate",
        JobType.Callback => "Callback",
        _ => "Job"
    };

    #region Drag and Drop

    private Point _dragStartPoint;

    private void OnJobCardMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private void OnJobCardMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        var mousePos = e.GetPosition(null);
        var diff = _dragStartPoint - mousePos;

        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            if (sender is Border card && card.Tag is Job job)
            {
                _draggedJob = job;
                var data = new DataObject("Job", job);
                DragDrop.DoDragDrop(card, data, DragDropEffects.Move);
            }
        }
    }

    private void OnJobDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("Job"))
        {
            e.Effects = DragDropEffects.None;
            return;
        }
        e.Effects = DragDropEffects.Move;
    }

    private async void OnJobDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("Job") || _draggedJob == null)
            return;

        // Determine target status based on column
        var targetStatus = sender switch
        {
            ItemsControl c when c == DraftColumn => JobStatus.Draft,
            ItemsControl c when c == ScheduledColumn => JobStatus.Scheduled,
            ItemsControl c when c == EnRouteColumn => JobStatus.EnRoute,
            ItemsControl c when c == InProgressColumn => JobStatus.InProgress,
            ItemsControl c when c == CompletedColumn => JobStatus.Completed,
            ItemsControl c when c == OnHoldColumn => JobStatus.OnHold,
            _ => _draggedJob.Status
        };

        if (_draggedJob.Status == targetStatus)
            return;

        // Update job status
        var oldStatus = _draggedJob.Status;
        _draggedJob.Status = targetStatus;

        // Update timestamps based on status change
        switch (targetStatus)
        {
            case JobStatus.EnRoute:
                _draggedJob.EnRouteAt = DateTime.Now;
                break;
            case JobStatus.InProgress:
                _draggedJob.ArrivedAt = DateTime.Now;
                break;
            case JobStatus.Completed:
                _draggedJob.CompletedAt = DateTime.Now;
                break;
        }

        try
        {
            await _dbContext.SaveChangesAsync();
            ToastService.Success($"Job moved to {targetStatus}");
            ApplyFiltersAndRefresh();
        }
        catch (Exception ex)
        {
            _draggedJob.Status = oldStatus;
            ToastService.Error($"Failed to update job: {ex.Message}");
        }
        finally
        {
            _draggedJob = null;
        }
    }

    #endregion

    #region Event Handlers

    private void OnJobCardDoubleClickCommand(Job job)
    {
        // Open drawer for editing
        var formContent = new Controls.JobFormContent(_dbContext);
        _ = formContent.LoadJob(job);
        
        _ = DrawerService.Instance.OpenDrawerAsync(
            title: "Edit Job",
            content: formContent,
            saveButtonText: "Save Changes",
            onSave: async () =>
            {
                if (!formContent.Validate())
                {
                    MessageBox.Show("Job title and Customer are required", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    var updated = formContent.GetJob();
                    job.Title = updated.Title;
                    job.CustomerId = updated.CustomerId;
                    job.SiteId = updated.SiteId;
                    job.ScheduledDate = updated.ScheduledDate;
                    job.JobType = updated.JobType;
                    job.Priority = updated.Priority;
                    job.Description = updated.Description;
                    
                    await _dbContext.SaveChangesAsync();
                    await DrawerService.Instance.CompleteDrawerAsync();
                    await LoadJobsAsync();
                    
                    ToastService.Success("Job updated!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to update job: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        );
        return;
        
        // Old code below - keeping for reference
        var detailDialog = new Dialogs.JobDetailDialog(_dbContext, job);
        detailDialog.ShowDialog();
        
        // Refresh if job was edited
        if (detailDialog.JobWasEdited)
        {
            _ = LoadJobsAsync();
        }
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await LoadJobsAsync();
        ToastService.Success("Jobs refreshed");
    }

    private void OnAddJobClick(object sender, RoutedEventArgs e)
    {
        var formContent = new Controls.JobFormContent(_dbContext);
        
        _ = DrawerService.Instance.OpenDrawerAsync(
            title: "Add Job",
            content: formContent,
            saveButtonText: "Create Job",
            onSave: async () =>
            {
                if (!formContent.Validate())
                {
                    MessageBox.Show("Job title and Customer are required", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    var job = formContent.GetJob();
                    _dbContext.Jobs.Add(job);
                    await _dbContext.SaveChangesAsync();
                    
                    await DrawerService.Instance.CompleteDrawerAsync();
                    await LoadJobsAsync();
                    
                    ToastService.Success($"Job '{job.Title}' created!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save job: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        );
    }

    private void OnDateFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            ApplyFiltersAndRefresh();
    }

    private void OnPriorityFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            ApplyFiltersAndRefresh();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded)
            ApplyFiltersAndRefresh();
    }

    #endregion
}
