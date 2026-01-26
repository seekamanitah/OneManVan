using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Controls.Shapes;
using OneManVan.Mobile.Helpers;
using OneManVan.Mobile.Theme;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

/// <summary>
/// Schedule-first job list page for field technicians.
/// Defaults to "Today" filter to show current day's schedule immediately.
/// </summary>
public partial class JobListPage : ContentPage
{
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;
    private CancellationTokenSource? _cts;
    private List<Job> _allJobs = [];
    private JobListFilter _activeFilter = JobListFilter.Today;

    public JobListPage(IDbContextFactory<OneManVanDbContext> dbFactory)
    {
        InitializeComponent();
        _dbFactory = dbFactory;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        
        try
        {
            UpdateDateHeader();
            await LoadJobsAsync(_cts.Token);
            
            // Ensure Today filter is visually active on load
            SetActiveFilterButton(TodayFilter);
        }
        catch (OperationCanceledException)
        {
            // Page navigated away - ignore
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"JobListPage.OnAppearing error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts?.Cancel();
    }

    private void UpdateDateHeader()
    {
        DateHeaderLabel.Text = "Today";
        DateSubHeaderLabel.Text = DateTime.Today.ToLongDate();
    }

    private async Task LoadJobsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Show loading indicator
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            
            // PERF: AsNoTracking + removed Asset include (not displayed)
            _allJobs = await db.Jobs
                .AsNoTracking()
                .Include(j => j.Customer)
                .Include(j => j.Site)
                .OrderBy(j => j.ScheduledDate)
                .ThenBy(j => j.Priority)
                .ToListAsync(cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                ApplyFilters();
                UpdateStats();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when navigating away
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                await DisplayAlertAsync("Error", $"Failed to load jobs: {ex.Message}", "OK");
            }
        }
        finally
        {
            // Hide loading indicator
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allJobs.AsEnumerable();
        var today = DateTime.Today;

        filtered = _activeFilter switch
        {
            JobListFilter.Today => filtered.Where(j => 
                j.ScheduledDate.HasValue && 
                j.ScheduledDate.Value.Date == today &&
                j.Status != JobStatus.Cancelled),
            
            JobListFilter.Scheduled => filtered.Where(j => 
                j.Status == JobStatus.Scheduled || 
                j.Status == JobStatus.Draft),
            
            JobListFilter.InProgress => filtered.Where(j => 
                j.Status == JobStatus.InProgress || 
                j.Status == JobStatus.EnRoute),
            
            JobListFilter.Completed => filtered.Where(j => 
                j.Status == JobStatus.Completed || 
                j.Status == JobStatus.Closed),
            
            JobListFilter.All => filtered.Where(j => 
                j.Status != JobStatus.Cancelled),
            
            _ => filtered
        };

        // Sort by scheduled time for Today view, or by date for others
        if (_activeFilter == JobListFilter.Today)
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

        JobCollection.ItemsSource = filtered.ToList();
    }

    private void UpdateStats()
    {
        var today = DateTime.Today;
        
        var todayCount = _allJobs.Count(j => 
            j.ScheduledDate.HasValue && 
            j.ScheduledDate.Value.Date == today &&
            j.Status != JobStatus.Cancelled);
        
        var activeCount = _allJobs.Count(j => 
            j.Status == JobStatus.Scheduled || 
            j.Status == JobStatus.EnRoute || 
            j.Status == JobStatus.InProgress);
        
        var completedTodayCount = _allJobs.Count(j => 
            j.CompletedAt.HasValue && 
            j.CompletedAt.Value.Date == today);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            TodayCountLabel.Text = todayCount.ToString();
            ActiveCountLabel.Text = activeCount.ToString();
            CompletedCountLabel.Text = completedTodayCount.ToString();
        });
    }

    #region Filter Handling

    private void OnFilterClicked(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            SetActiveFilterButton(button);

            _activeFilter = button.Text switch
            {
                "Today" => JobListFilter.Today,
                "Scheduled" => JobListFilter.Scheduled,
                "In Progress" => JobListFilter.InProgress,
                "Completed" => JobListFilter.Completed,
                "All" => JobListFilter.All,
                _ => JobListFilter.Today
            };

            // Update header for non-Today filters
            DateHeaderLabel.Text = _activeFilter switch
            {
                JobListFilter.Today => "Today",
                JobListFilter.Scheduled => "Scheduled Jobs",
                JobListFilter.InProgress => "In Progress",
                JobListFilter.Completed => "Completed",
                JobListFilter.All => "All Jobs",
                _ => "Jobs"
            };

            ApplyFilters();
        }
    }

    private void SetActiveFilterButton(Button activeButton)
    {
        // Reset all filter button styles using theme colors
        TodayFilter.BackgroundColor = AppColors.PrimarySurface;
        TodayFilter.TextColor = AppColors.Primary;
        
        ScheduledFilter.BackgroundColor = AppColors.PrimarySurface;
        ScheduledFilter.TextColor = AppColors.Primary;
        
        InProgressFilter.BackgroundColor = AppColors.WarningSurface;
        InProgressFilter.TextColor = AppColors.Warning;
        
        CompletedFilter.BackgroundColor = AppColors.SuccessSurface;
        CompletedFilter.TextColor = AppColors.Success;
        
        AllFilter.BackgroundColor = AppColors.FilterInactive;
        AllFilter.TextColor = AppColors.FilterInactiveText;

        // Highlight active filter
        activeButton.BackgroundColor = AppColors.Primary;
        activeButton.TextColor = AppColors.TextOnDark;
    }

    #endregion

    #region Calendar

    private async void OnCalendarClicked(object sender, EventArgs e)
    {
        // Show date picker to jump to a specific date
        var options = new[]
        {
            "Today",
            "Tomorrow",
            "This Week",
            "Next Week",
            "Pick Date..."
        };

        var result = await DisplayActionSheetAsync("Select Date", "Cancel", null, options);

        if (string.IsNullOrEmpty(result) || result == "Cancel") return;

        var targetDate = result switch
        {
            "Today" => DateTime.Today,
            "Tomorrow" => DateTime.Today.AddDays(1),
            "This Week" => DateTime.Today,
            "Next Week" => DateTime.Today.AddDays(7 - (int)DateTime.Today.DayOfWeek + 1),
            _ => DateTime.Today
        };

        if (result == "Pick Date...")
        {
            // Show custom date picker popup
            var customDate = await ShowCustomDatePickerAsync();
            if (customDate.HasValue)
            {
                targetDate = customDate.Value;
            }
            else
            {
                // User cancelled - return without updating
                return;
            }
        }

        // Update the view for the selected date
        _activeFilter = JobListFilter.Today;
        SetActiveFilterButton(TodayFilter);
        
        DateHeaderLabel.Text = targetDate == DateTime.Today ? "Today" : targetDate.ToDayOfWeek();
        DateSubHeaderLabel.Text = targetDate.ToLongDate();
        
        // Filter to selected date
        var filtered = _allJobs
            .Where(j => j.ScheduledDate.HasValue && 
                        j.ScheduledDate.Value.Date == targetDate &&
                        j.Status != JobStatus.Cancelled)
            .OrderBy(j => j.ScheduledDate)
            .ToList();
        
        JobCollection.ItemsSource = filtered;
    }

    #endregion

    #region Job Selection and Navigation

    private async void OnJobSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Job job)
        {
            JobCollection.SelectedItem = null;
            await Navigation.PushAsync(new JobDetailPage(job.Id));
        }
    }

    private async void OnNavigateClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Job job)
        {
            if (job.Site == null)
            {
                await DisplayAlertAsync("No Address", "This job has no address on file.", "OK");
                return;
            }

            try
            {
                if (job.Site.Latitude.HasValue && job.Site.Longitude.HasValue)
                {
                    var location = new Location((double)job.Site.Latitude.Value, (double)job.Site.Longitude.Value);
                    await Map.Default.OpenAsync(location, new MapLaunchOptions
                    {
                        NavigationMode = NavigationMode.Driving
                    });
                }
                else
                {
                    var address = $"{job.Site.Address}, {job.Site.City}, {job.Site.State} {job.Site.ZipCode}";
                    var uri = new Uri($"geo:0,0?q={Uri.EscapeDataString(address)}");
                    await Launcher.Default.OpenAsync(uri);
                }

                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Navigation Error", $"Could not open maps: {ex.Message}", "OK");
            }
        }
    }

    #endregion

    #region Swipe Actions

    private async void OnStartSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is Job job)
        {
            if (job.Status == JobStatus.Completed || job.Status == JobStatus.Closed)
            {
                await DisplayAlertAsync("Already Complete", "This job is already completed.", "OK");
                return;
            }

            var confirm = await DisplayAlertAsync("Start Job", 
                $"Start working on '{job.Title}'?", "Start", "Cancel");

            if (confirm)
            {
                try
                {
                    await using var db = await _dbFactory.CreateDbContextAsync();
                    var dbJob = await db.Jobs.FindAsync(job.Id);
                    if (dbJob != null)
                    {
                        dbJob.Status = JobStatus.InProgress;
                        dbJob.StartedAt ??= DateTime.Now;
                        dbJob.UpdatedAt = DateTime.Now;
                        await db.SaveChangesAsync();
                        
                        // Update local copy
                        job.Status = dbJob.Status;
                        job.StartedAt = dbJob.StartedAt;
                        job.UpdatedAt = dbJob.UpdatedAt;
                    }
                    
                    try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
                    
                    ApplyFilters();
                    UpdateStats();
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Error", $"Failed to start job: {ex.Message}", "OK");
                }
            }
        }
    }

    private async void OnCompleteSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is Job job)
        {
            if (job.Status == JobStatus.Completed || job.Status == JobStatus.Closed)
            {
                await DisplayAlertAsync("Already Complete", "This job is already completed.", "OK");
                return;
            }

            var confirm = await DisplayAlertAsync("Complete Job", 
                $"Mark '{job.Title}' as completed?", "Complete", "Cancel");

            if (confirm)
            {
                try
                {
                    await using var db = await _dbFactory.CreateDbContextAsync();
                    var dbJob = await db.Jobs.FindAsync(job.Id);
                    if (dbJob != null)
                    {
                        dbJob.Status = JobStatus.Completed;
                        dbJob.StartedAt ??= DateTime.Now;
                        dbJob.CompletedAt = DateTime.Now;
                        dbJob.UpdatedAt = DateTime.Now;
                        await db.SaveChangesAsync();
                        
                        // Update local copy
                        job.Status = dbJob.Status;
                        job.StartedAt = dbJob.StartedAt;
                        job.CompletedAt = dbJob.CompletedAt;
                        job.UpdatedAt = dbJob.UpdatedAt;
                    }
                    
                    try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
                    
                    ApplyFilters();
                    UpdateStats();

                    var createInvoice = await DisplayAlertAsync("Job Completed", 
                        "Would you like to create an invoice now?", "Yes", "Later");
                    
                    if (createInvoice)
                    {
                        await Navigation.PushAsync(new JobDetailPage(job.Id));
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Error", $"Failed to complete job: {ex.Message}", "OK");
                }
            }
        }
    }

    #endregion

    #region Add Job

    private async void OnAddJobClicked(object sender, EventArgs e)
    {
        // Navigate to full AddJobPage for complete job creation
        await Shell.Current.GoToAsync("AddJob");
    }

    #endregion

    #region Refresh

    private async void OnRefreshing(object sender, EventArgs e)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        await LoadJobsAsync(_cts.Token);
        RefreshViewControl.IsRefreshing = false;
    }

    #endregion

    #region Custom Date Picker

    private async Task<DateTime?> ShowCustomDatePickerAsync()
    {
        // Create a popup with date picker
        var popup = new ContentPage
        {
            BackgroundColor = Colors.Transparent
        };

        var overlay = new Grid
        {
            BackgroundColor = Color.FromArgb("#80000000"), // Semi-transparent black
            Padding = 20
        };

        var card = new Border
        {
            BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark 
                ? Color.FromArgb("#2D2D2D") 
                : Colors.White,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(16) },
            Stroke = Colors.Transparent,
            Padding = 20,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            MaximumWidthRequest = 400,
            Shadow = new Shadow
            {
                Brush = Colors.Black,
                Offset = new Point(0, 4),
                Radius = 8,
                Opacity = 0.3f
            }
        };

        var cardContent = new VerticalStackLayout
        {
            Spacing = 16
        };

        // Title
        var title = new Label
        {
            Text = "Select Date",
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            TextColor = Application.Current?.RequestedTheme == AppTheme.Dark 
                ? Colors.White 
                : Color.FromArgb("#333333"),
            HorizontalOptions = LayoutOptions.Center
        };

        // Date Picker
        var datePicker = new DatePicker
        {
            Date = DateTime.Today,
            MinimumDate = DateTime.Today.AddMonths(-12),
            MaximumDate = DateTime.Today.AddMonths(12),
            FontSize = 16,
            HorizontalOptions = LayoutOptions.Fill
        };

        // Helper text
        var helperText = new Label
        {
            Text = "Select a date to view jobs scheduled for that day",
            FontSize = 12,
            TextColor = Application.Current?.RequestedTheme == AppTheme.Dark 
                ? Color.FromArgb("#B0B0B0") 
                : Color.FromArgb("#757575"),
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };

        // Buttons
        var buttonGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            },
            ColumnSpacing = 12,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark 
                ? Color.FromArgb("#404040") 
                : Color.FromArgb("#E0E0E0"),
            TextColor = Application.Current?.RequestedTheme == AppTheme.Dark 
                ? Colors.White 
                : Color.FromArgb("#333333"),
            CornerRadius = 8,
            HeightRequest = 44
        };

        var selectButton = new Button
        {
            Text = "Select",
            BackgroundColor = Color.FromArgb("#1976D2"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8,
            HeightRequest = 44
        };

        buttonGrid.Add(cancelButton, 0, 0);
        buttonGrid.Add(selectButton, 1, 0);

        cardContent.Children.Add(title);
        cardContent.Children.Add(datePicker);
        cardContent.Children.Add(helperText);
        cardContent.Children.Add(buttonGrid);

        card.Content = cardContent;
        overlay.Children.Add(card);
        popup.Content = overlay;

        // Handle button clicks
        DateTime? selectedDate = null;
        var tcs = new TaskCompletionSource<DateTime?>();

        cancelButton.Clicked += (s, e) =>
        {
            tcs.TrySetResult(null);
        };

        selectButton.Clicked += (s, e) =>
        {
            selectedDate = datePicker.Date;
            tcs.TrySetResult(selectedDate);
        };

        // Show popup
        await Navigation.PushModalAsync(popup, animated: true);

        // Wait for user selection
        var result = await tcs.Task;

        // Close popup
        await Navigation.PopModalAsync(animated: true);

        return result;
    }

    #endregion
}

/// <summary>
/// Filter options for job list view.
/// </summary>
public enum JobListFilter
{
    Today,
    Scheduled,
    InProgress,
    Completed,
    All
}
