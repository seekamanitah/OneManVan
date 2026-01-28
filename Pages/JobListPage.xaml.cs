using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using OneManVan.Services;

namespace OneManVan.Pages;

/// <summary>
/// Schedule-first job list page for desktop.
/// Defaults to "Today" filter to show current day's schedule immediately.
/// </summary>
public partial class JobListPage : UserControl
{
    private readonly OneManVanDbContext _dbContext;
    private List<Job> _allJobs = [];
    private Job? _selectedJob;
    private DateTime _viewDate = DateTime.Today;

    public JobListPage()
    {
        InitializeComponent();
        _dbContext = App.DbContext;
        
        UpdateDateHeader();
        Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        await LoadJobsAsync();
    }

    private void UpdateDateHeader()
    {
        if (_viewDate.Date == DateTime.Today)
        {
            DateHeaderText.Text = "Today";
        }
        else if (_viewDate.Date == DateTime.Today.AddDays(1))
        {
            DateHeaderText.Text = "Tomorrow";
        }
        else if (_viewDate.Date == DateTime.Today.AddDays(-1))
        {
            DateHeaderText.Text = "Yesterday";
        }
        else
        {
            DateHeaderText.Text = _viewDate.ToString("dddd");
        }
        DateSubHeaderText.Text = _viewDate.ToString("MMMM d, yyyy");
    }

    private async Task LoadJobsAsync()
    {
        try
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            EmptyState.Visibility = Visibility.Collapsed;

            _allJobs = await _dbContext.Jobs
                .Include(j => j.Customer)
                .Include(j => j.Site)
                .Include(j => j.Asset)
                .OrderBy(j => j.ScheduledDate)
                .ThenByDescending(j => j.Priority)
                .ToListAsync();

            ApplyFilters();
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

    private void ApplyFilters()
    {
        var filtered = _allJobs.AsEnumerable();

        // Apply filter based on selected radio button
        if (TodayFilter.IsChecked == true)
        {
            filtered = filtered.Where(j => 
                j.ScheduledDate.HasValue && 
                j.ScheduledDate.Value.Date == _viewDate.Date &&
                j.Status != JobStatus.Cancelled);
        }
        else if (ScheduledFilter.IsChecked == true)
        {
            filtered = filtered.Where(j => 
                j.Status == JobStatus.Scheduled || 
                j.Status == JobStatus.Draft);
        }
        else if (InProgressFilter.IsChecked == true)
        {
            filtered = filtered.Where(j => 
                j.Status == JobStatus.InProgress || 
                j.Status == JobStatus.EnRoute);
        }
        else if (CompletedFilter.IsChecked == true)
        {
            filtered = filtered.Where(j => 
                j.Status == JobStatus.Completed || 
                j.Status == JobStatus.Closed);
        }
        else // All
        {
            filtered = filtered.Where(j => j.Status != JobStatus.Cancelled);
        }

        // Apply search filter
        var searchText = SearchBox.Text?.Trim().ToLower() ?? "";
        if (!string.IsNullOrEmpty(searchText))
        {
            filtered = filtered.Where(j =>
                (j.Title?.ToLower().Contains(searchText) ?? false) ||
                (j.JobNumber?.ToLower().Contains(searchText) ?? false) ||
                (j.Customer?.Name?.ToLower().Contains(searchText) ?? false) ||
                (j.Site?.Address?.ToLower().Contains(searchText) ?? false) ||
                (j.Description?.ToLower().Contains(searchText) ?? false));
        }

        // Sort: Today view by time, others by date
        if (TodayFilter.IsChecked == true)
        {
            filtered = filtered
                .OrderBy(j => j.Status == JobStatus.Completed || j.Status == JobStatus.Closed)
                .ThenBy(j => j.ScheduledDate);
        }
        else
        {
            filtered = filtered
                .OrderByDescending(j => j.ScheduledDate ?? DateTime.MinValue)
                .ThenBy(j => j.Status);
        }

        var jobList = filtered.ToList();
        JobListView.ItemsSource = jobList;

        // Update count
        JobCountText.Text = $"{jobList.Count} jobs";

        // Show empty state if needed
        EmptyState.Visibility = jobList.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnFilterChanged(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            ApplyFilters();
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded)
        {
            ApplyFilters();
        }
    }

    private void OnJobSelected(object sender, SelectionChangedEventArgs e)
    {
        _selectedJob = JobListView.SelectedItem as Job;
        
        if (_selectedJob != null)
        {
            // Note: NoSelectionPanel and JobDetailsPanel removed - using drawer system now
            
            // Open drawer for editing
            var formContent = new Controls.JobFormContent(_dbContext);
            _ = formContent.LoadJob(_selectedJob);
            
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
                        _selectedJob.Title = updated.Title;
                        _selectedJob.CustomerId = updated.CustomerId;
                        _selectedJob.SiteId = updated.SiteId;
                        _selectedJob.ScheduledDate = updated.ScheduledDate;
                        _selectedJob.JobType = updated.JobType;
                        _selectedJob.Priority = updated.Priority;
                        _selectedJob.Description = updated.Description;
                        
                        await _dbContext.SaveChangesAsync();
                        await DrawerService.Instance.CompleteDrawerAsync();
                        await LoadJobsAsync();
                        
                        MessageBox.Show($"Job '{_selectedJob.Title}' updated!", "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to update job: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            );
        }
    }

    // UpdateJobDetails method removed - no longer needed with drawer system
    // Details are shown in the drawer, not in a side panel

    private void UpdateActionButtons()
    {
        // Method stub - no action buttons in the simplified list view
        // Actions are now in the drawer
    }

    private static Color GetStatusColor(JobStatus status) => status switch
    {
        JobStatus.Draft => Color.FromRgb(107, 114, 128),
        JobStatus.Scheduled => Color.FromRgb(59, 130, 246),
        JobStatus.EnRoute => Color.FromRgb(139, 92, 246),
        JobStatus.InProgress => Color.FromRgb(245, 158, 11),
        JobStatus.Completed => Color.FromRgb(16, 185, 129),
        JobStatus.Closed => Color.FromRgb(99, 102, 241),
        JobStatus.Cancelled => Color.FromRgb(239, 68, 68),
        JobStatus.OnHold => Color.FromRgb(245, 158, 11),
        _ => Color.FromRgb(107, 114, 128)
    };

    #region Date Navigation

    private void OnPreviousDayClick(object sender, RoutedEventArgs e)
    {
        _viewDate = _viewDate.AddDays(-1);
        UpdateDateHeader();
        if (TodayFilter.IsChecked == true)
        {
            ApplyFilters();
        }
    }

    private void OnTodayClick(object sender, RoutedEventArgs e)
    {
        _viewDate = DateTime.Today;
        UpdateDateHeader();
        TodayFilter.IsChecked = true;
        ApplyFilters();
    }

    private void OnNextDayClick(object sender, RoutedEventArgs e)
    {
        _viewDate = _viewDate.AddDays(1);
        UpdateDateHeader();
        if (TodayFilter.IsChecked == true)
        {
            ApplyFilters();
        }
    }

    #endregion

    #region Job Actions


    private async void OnStatusChanged(object sender, SelectionChangedEventArgs e)
    {
        // Status change now handled in the drawer/dialog, not in the list view
        await Task.CompletedTask;
    }

    private async void OnStartJobClick(object sender, RoutedEventArgs e)
    {
        if (_selectedJob == null) return;

        try
        {
            if (_selectedJob.Status == JobStatus.Draft || _selectedJob.Status == JobStatus.Scheduled)
            {
                _selectedJob.Status = JobStatus.InProgress;
                _selectedJob.StartedAt = DateTime.Now;
            }
            else if (_selectedJob.Status == JobStatus.EnRoute)
            {
                _selectedJob.Status = JobStatus.InProgress;
                _selectedJob.ArrivedAt = DateTime.Now;
            }

            _selectedJob.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            
            ToastService.Success("Job started");
            ApplyFilters();
        }
        catch (Exception ex)
        {
            ToastService.Error($"Failed to start job: {ex.Message}");
        }
    }

    private async void OnCompleteJobClick(object sender, RoutedEventArgs e)
    {
        if (_selectedJob == null) return;

        var result = MessageBox.Show(
            $"Mark '{_selectedJob.Title}' as completed?",
            "Complete Job",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            _selectedJob.Status = JobStatus.Completed;
            _selectedJob.StartedAt ??= DateTime.Now;
            _selectedJob.CompletedAt = DateTime.Now;
            _selectedJob.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            
            ToastService.Success("Job completed");
            ApplyFilters();
        }
        catch (Exception ex)
        {
            ToastService.Error($"Failed to complete job: {ex.Message}");
        }
    }

    private void OnCreateInvoiceClick(object sender, RoutedEventArgs e)
    {
        if (_selectedJob == null) return;

        // Navigate to invoices page
        NavigationRequest.Navigate("Invoices", "FromJob");
        ToastService.Info("Navigate to Invoices to create an invoice for this job");
    }

    private void OnEditJobClick(object sender, RoutedEventArgs e)
    {
        if (_selectedJob == null) return;
        ToastService.Info("Job editing coming soon");
    }

    private async void OnDeleteJobClick(object sender, RoutedEventArgs e)
    {
        if (_selectedJob == null) return;

        var result = MessageBox.Show(
            $"Are you sure you want to delete '{_selectedJob.Title}'?\n\nThis action cannot be undone.",
            "Delete Job",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            _dbContext.Jobs.Remove(_selectedJob);
            await _dbContext.SaveChangesAsync();

            _selectedJob = null;
            JobListView.SelectedItem = null;

            await LoadJobsAsync();
            MessageBox.Show("Job deleted", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ToastService.Error($"Failed to delete job: {ex.Message}");
        }
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
                    
                    ToastService.Success($"Job '{job.Title}' created successfully!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save job: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        );
    }

    private void OnKanbanViewClick(object sender, RoutedEventArgs e)
    {
        NavigationRequest.Navigate("JobKanban");
    }

    #endregion
}
