using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Controls;
using OneManVan.Mobile.Services;
using OneManVan.Mobile.Theme;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile;

public partial class MainPage : ContentPage
{
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;
    private readonly TradeConfigurationService _tradeService;
    private CancellationTokenSource? _cts;
    private int _currentDashboard = 1; // 1 = Today, 2 = Week, 3 = Month
    private DateTime _weekStartDate;
    private DateTime _selectedCalendarDate;
    private Job? _nextJob;
    private List<Job> _todayJobs = [];
    private List<Job> _selectedDayJobs = [];
    private Dictionary<DateTime, int> _monthJobCounts = [];

    public MainPage(IDbContextFactory<OneManVanDbContext> dbFactory, TradeConfigurationService tradeService)
    {
        InitializeComponent();
        _dbFactory = dbFactory;
        _tradeService = tradeService;
        
        // Initialize to current week (starting Monday)
        var today = DateTime.Today;
        var daysUntilMonday = ((int)today.DayOfWeek - 1 + 7) % 7;
        _weekStartDate = today.AddDays(-daysUntilMonday);
        _selectedCalendarDate = today;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        
        try
        {
            UpdateTradeLabels();
            UpdateDateTimeHeader();
            
            await LoadScheduleFirstDashboardAsync(_cts.Token);
            
            if (_currentDashboard == 2)
            {
                await LoadWeekDashboardAsync(_cts.Token);
            }
            else if (_currentDashboard == 3)
            {
                await LoadMonthDashboardAsync(_cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Page was navigated away
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainPage.OnAppearing error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts?.Cancel();
    }

    private void UpdateTradeLabels()
    {
        var preset = _tradeService.CurrentPreset;
        
        var searchBtn = this.FindByName<Button>("SearchAssetsButton");
        if (searchBtn != null)
        {
            searchBtn.Text = $"Search {preset.AssetPluralLabel}";
        }
    }

    private void UpdateDateTimeHeader()
    {
        TodayDayLabel.Text = "Today";
        TodayDateLabel.Text = DateTime.Today.ToString("dddd, MMMM d, yyyy");
        TodayTimeLabel.Text = DateTime.Now.ToString("h:mm tt");
    }

    #region Dashboard Toggle

    private async void OnDashboardToggleClicked(object sender, EventArgs e)
    {
        // Cycle through: Today (1) -> Week (2) -> Month (3) -> Today (1)
        _currentDashboard = _currentDashboard switch
        {
            1 => 2,
            2 => 3,
            3 => 1,
            _ => 1
        };
        
        Dashboard1.IsVisible = _currentDashboard == 1;
        Dashboard2.IsVisible = _currentDashboard == 2;
        Dashboard3.IsVisible = _currentDashboard == 3;
        
        DashboardToggle.Text = _currentDashboard switch
        {
            1 => "Week",
            2 => "Month",
            3 => "Today",
            _ => "Week"
        };
        
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        
        if (_currentDashboard == 2)
        {
            await LoadWeekDashboardAsync(_cts.Token);
        }
        else if (_currentDashboard == 3)
        {
            await LoadMonthDashboardAsync(_cts.Token);
        }
        
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }

    #endregion

    #region Dashboard 1: Schedule-First View

    private async Task LoadScheduleFirstDashboardAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            
            var today = DateTime.Today;
            var now = DateTime.Now;
            var weekStart = today.AddDays(-(int)today.DayOfWeek + 1);
            var weekEnd = weekStart.AddDays(6);

            // Get today's jobs (PERF: AsNoTracking)
            _todayJobs = await db.Jobs
                .AsNoTracking()
                .Include(j => j.Customer)
                .Include(j => j.Site)
                .Where(j => j.ScheduledDate.HasValue &&
                           j.ScheduledDate.Value.Date == today &&
                           j.Status != JobStatus.Cancelled)
                .OrderBy(j => j.ScheduledDate)
                .ToListAsync(cancellationToken);

            // Get week stats
            var weekJobCount = await db.Jobs
                .Where(j => j.ScheduledDate.HasValue &&
                           j.ScheduledDate.Value.Date >= weekStart &&
                           j.ScheduledDate.Value.Date <= weekEnd &&
                           j.Status != JobStatus.Cancelled)
                .CountAsync(cancellationToken);

            var overdueCount = await db.Jobs
                .Where(j => j.ScheduledDate.HasValue &&
                           j.ScheduledDate.Value.Date < today &&
                           j.Status != JobStatus.Completed &&
                           j.Status != JobStatus.Closed &&
                           j.Status != JobStatus.Cancelled)
                .CountAsync(cancellationToken);

            // Find next job (not completed, earliest scheduled)
            _nextJob = _todayJobs
                .Where(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Closed)
                .OrderBy(j => j.ScheduledDate)
                .ThenBy(j => j.Priority)
                .FirstOrDefault();

            if (!cancellationToken.IsCancellationRequested)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Update quick stats
                    TodayJobCountLabel.Text = _todayJobs.Count.ToString();
                    WeekJobCountLabel.Text = weekJobCount.ToString();
                    OverdueCountLabel.Text = overdueCount.ToString();

                    // Update next job card
                    UpdateNextJobCard();

                    // Build today's job list
                    BuildTodayJobsList();
                });
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                await DisplayAlertAsync("Error", $"Failed to load dashboard: {ex.Message}", "OK");
            }
        }
    }

    private void UpdateNextJobCard()
    {
        if (_nextJob == null)
        {
            NextJobCard.IsVisible = false;
            NoJobsCard.IsVisible = true;
            return;
        }

        NextJobCard.IsVisible = true;
        NoJobsCard.IsVisible = false;

        // Update card content
        NextJobTimeLabel.Text = _nextJob.ScheduledDate?.ToString("h:mm tt") ?? "TBD";
        NextJobTitleLabel.Text = _nextJob.Title;
        NextJobCustomerLabel.Text = _nextJob.Customer?.Name ?? "Unknown Customer";
        NextJobStatusLabel.Text = _nextJob.StatusDisplay;

        // Address
        if (_nextJob.Site != null)
        {
            NextJobAddressLabel.Text = $"{_nextJob.Site.Address}, {_nextJob.Site.City}";
        }
        else
        {
            NextJobAddressLabel.Text = "No address on file";
        }

        // Status color
        var statusColor = _nextJob.Status switch
        {
            JobStatus.Scheduled => Color.FromArgb("#2196F3"),
            JobStatus.EnRoute => Color.FromArgb("#FF9800"),
            JobStatus.InProgress => Color.FromArgb("#FF9800"),
            JobStatus.Completed => Color.FromArgb("#4CAF50"),
            JobStatus.Closed => Color.FromArgb("#4CAF50"),
            _ => Color.FromArgb("#757575")
        };
        
        NextJobStatusBar.Color = statusColor;
        NextJobStatusBadge.BackgroundColor = statusColor;

        // Update button based on status
        StartNextJobButton.Text = _nextJob.Status switch
        {
            JobStatus.Scheduled => "Start Job",
            JobStatus.EnRoute => "Arrived",
            JobStatus.InProgress => "View Details",
            _ => "View Details"
        };
    }

    private void BuildTodayJobsList()
    {
        TodayJobsList.Children.Clear();

        // Skip the first job since it's shown in the "Next Up" card
        var remainingJobs = _todayJobs.Skip(_nextJob != null ? 1 : 0).Take(5).ToList();

        if (!remainingJobs.Any())
        {
            if (_todayJobs.Count <= 1)
            {
                TodayJobsList.Children.Add(new Label
                {
                    Text = _todayJobs.Any() ? "No other jobs today" : "No jobs scheduled",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#9E9E9E"),
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 8)
                });
            }
            return;
        }

        foreach (var job in remainingJobs)
        {
            TodayJobsList.Children.Add(CreateJobCard(job));
        }

        if (_todayJobs.Count > 6)
        {
            TodayJobsList.Children.Add(new Label
            {
                Text = $"+{_todayJobs.Count - 6} more jobs...",
                FontSize = 12,
                TextColor = Color.FromArgb("#2196F3"),
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 4)
            });
        }
    }

    private Border CreateJobCard(Job job)
    {
        var isCompleted = job.Status == JobStatus.Completed || job.Status == JobStatus.Closed;
        
        var card = new Border
        {
            BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark 
                ? Color.FromArgb("#2D2D2D") 
                : Colors.White,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Stroke = Colors.Transparent,
            Padding = 0
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(new GridLength(4)),
                new ColumnDefinition(GridLength.Star)
            }
        };

        // Status bar
        var statusColor = job.Status switch
        {
            JobStatus.Scheduled => Color.FromArgb("#2196F3"),
            JobStatus.EnRoute => Color.FromArgb("#FF9800"),
            JobStatus.InProgress => Color.FromArgb("#FF9800"),
            JobStatus.Completed => Color.FromArgb("#4CAF50"),
            JobStatus.Closed => Color.FromArgb("#4CAF50"),
            _ => Color.FromArgb("#757575")
        };

        var statusBar = new BoxView
        {
            Color = statusColor,
            WidthRequest = 4,
            VerticalOptions = LayoutOptions.Fill
        };
        Grid.SetColumn(statusBar, 0);
        grid.Children.Add(statusBar);

        var content = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            Padding = new Thickness(12, 10)
        };

        // Time
        var timeLabel = new Label
        {
            Text = job.ScheduledDate?.ToString("h:mm") ?? "TBD",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = isCompleted ? Color.FromArgb("#9E9E9E") : statusColor,
            VerticalOptions = LayoutOptions.Center
        };
        Grid.SetColumn(timeLabel, 0);
        content.Children.Add(timeLabel);

        // Title and customer
        var infoStack = new VerticalStackLayout
        {
            Margin = new Thickness(12, 0),
            Spacing = 2
        };
        infoStack.Children.Add(new Label
        {
            Text = job.Title,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = isCompleted 
                ? Color.FromArgb("#9E9E9E") 
                : (Application.Current?.RequestedTheme == AppTheme.Dark ? Colors.White : Color.FromArgb("#333")),
            LineBreakMode = LineBreakMode.TailTruncation,
            TextDecorations = isCompleted ? TextDecorations.Strikethrough : TextDecorations.None
        });
        infoStack.Children.Add(new Label
        {
            Text = job.Customer?.Name ?? "Unknown",
            FontSize = 12,
            TextColor = Color.FromArgb("#9E9E9E"),
            LineBreakMode = LineBreakMode.TailTruncation
        });
        Grid.SetColumn(infoStack, 1);
        content.Children.Add(infoStack);

        // Status badge
        var badge = new Border
        {
            BackgroundColor = statusColor,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
            Stroke = Colors.Transparent,
            Padding = new Thickness(8, 4),
            VerticalOptions = LayoutOptions.Center
        };
        badge.Content = new Label
        {
            Text = job.StatusDisplay,
            FontSize = 9,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White
        };
        Grid.SetColumn(badge, 2);
        content.Children.Add(badge);

        Grid.SetColumn(content, 1);
        grid.Children.Add(content);

        card.Content = grid;

        // Tap to navigate
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (s, e) =>
        {
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            await Navigation.PushAsync(new OneManVan.Mobile.Pages.JobDetailPage(job.Id));
        };
        card.GestureRecognizers.Add(tapGesture);

        return card;
    }

    #endregion

    #region Next Job Actions

    private async void OnNavigateNextJobClicked(object sender, EventArgs e)
    {
        if (_nextJob?.Site == null)
        {
            await DisplayAlertAsync("No Address", "This job has no address on file.", "OK");
            return;
        }

        try
        {
            if (_nextJob.Site.Latitude.HasValue && _nextJob.Site.Longitude.HasValue)
            {
                var location = new Location((double)_nextJob.Site.Latitude.Value, (double)_nextJob.Site.Longitude.Value);
                await Map.Default.OpenAsync(location, new MapLaunchOptions
                {
                    NavigationMode = NavigationMode.Driving,
                    Name = _nextJob.Customer?.Name ?? "Job Site"
                });
            }
            else
            {
                var placemark = new Placemark
                {
                    Thoroughfare = _nextJob.Site.Address,
                    Locality = _nextJob.Site.City,
                    AdminArea = _nextJob.Site.State,
                    PostalCode = _nextJob.Site.ZipCode,
                    CountryName = "USA"
                };
                await Map.Default.OpenAsync(placemark, new MapLaunchOptions
                {
                    NavigationMode = NavigationMode.Driving,
                    Name = _nextJob.Customer?.Name ?? "Job Site"
                });
            }

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Navigation Error", $"Could not open maps: {ex.Message}", "OK");
        }
    }

    private async void OnStartNextJobClicked(object sender, EventArgs e)
    {
        if (_nextJob == null) return;

        if (_nextJob.Status == JobStatus.InProgress || 
            _nextJob.Status == JobStatus.Completed || 
            _nextJob.Status == JobStatus.Closed)
        {
            // Just navigate to detail page
            await Navigation.PushAsync(new OneManVan.Mobile.Pages.JobDetailPage(_nextJob.Id));
            return;
        }

        var confirm = await DisplayAlertAsync("Start Job", 
            $"Start working on '{_nextJob.Title}'?", "Start", "Cancel");

        if (confirm)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync();
                var dbJob = await db.Jobs.FindAsync(_nextJob.Id);
                if (dbJob != null)
                {
                    dbJob.Status = JobStatus.InProgress;
                    dbJob.StartedAt ??= DateTime.Now;
                    dbJob.UpdatedAt = DateTime.Now;
                    await db.SaveChangesAsync();
                }
                
                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

                // Navigate to job detail
                await Navigation.PushAsync(new OneManVan.Mobile.Pages.JobDetailPage(_nextJob.Id));
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed to start job: {ex.Message}", "OK");
            }
        }
    }

    private async void OnViewAllJobsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//JobsTab");
    }

    #endregion

    #region Dashboard 2: Week Overview

    private async void OnPreviousWeekClicked(object sender, EventArgs e)
    {
        _weekStartDate = _weekStartDate.AddDays(-7);
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        await LoadWeekDashboardAsync(_cts.Token);
    }

    private async void OnNextWeekClicked(object sender, EventArgs e)
    {
        _weekStartDate = _weekStartDate.AddDays(7);
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        await LoadWeekDashboardAsync(_cts.Token);
    }

    private async Task LoadWeekDashboardAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var weekEnd = _weekStartDate.AddDays(6);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeekRangeLabel.Text = $"{_weekStartDate:MMM d} - {weekEnd:MMM d}";
                WeekYearLabel.Text = _weekStartDate.Year.ToString();
            });

            await BuildWeekCalendarAsync(cancellationToken);

            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            
            var weekJobs = await db.Jobs
                .AsNoTracking()
                .Include(j => j.Customer)
                .Where(j => j.ScheduledDate.HasValue &&
                           j.ScheduledDate.Value.Date >= _weekStartDate &&
                           j.ScheduledDate.Value.Date <= weekEnd)
                .ToListAsync(cancellationToken);

            var completedThisWeek = weekJobs.Count(j => 
                j.Status == JobStatus.Completed || j.Status == JobStatus.Closed);
            
            var revenueThisWeek = weekJobs
                .Where(j => j.Status == JobStatus.Completed || j.Status == JobStatus.Closed)
                .Sum(j => j.Total);

            if (!cancellationToken.IsCancellationRequested)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    WeekJobsLabel.Text = weekJobs.Count.ToString();
                    WeekCompletedLabel.Text = completedThisWeek.ToString();
                    WeekRevenueLabel.Text = revenueThisWeek >= 1000 
                        ? $"${revenueThisWeek / 1000:F1}k" 
                        : $"${revenueThisWeek:F0}";
                });
            }

            await LoadToDoItemsAsync(weekEnd, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlertAsync("Error", $"Could not load week data: {ex.Message}", "OK");
                });
            }
        }
    }

    private async Task BuildWeekCalendarAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var jobCounts = new Dictionary<DateTime, int>();
        
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            
            var weekEnd = _weekStartDate.AddDays(6);
            var jobs = await db.Jobs
                .Where(j => j.ScheduledDate.HasValue &&
                           j.ScheduledDate.Value.Date >= _weekStartDate &&
                           j.ScheduledDate.Value.Date <= weekEnd &&
                           j.Status != JobStatus.Cancelled)
                .Select(j => j.ScheduledDate!.Value.Date)
                .ToListAsync(cancellationToken);
            
            foreach (var date in jobs)
            {
                if (jobCounts.ContainsKey(date))
                    jobCounts[date]++;
                else
                    jobCounts[date] = 1;
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch
        {
            // Continue with empty counts
        }

        if (cancellationToken.IsCancellationRequested) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            WeekCalendarGrid.Children.Clear();

            for (int i = 0; i < 7; i++)
            {
                var date = _weekStartDate.AddDays(i);
                var isToday = date == today;
                var isPast = date < today;

                var dayStack = new VerticalStackLayout
                {
                    HorizontalOptions = LayoutOptions.Center,
                    Spacing = 2
                };

                dayStack.Children.Add(new Label
                {
                    Text = date.ToString("ddd").ToUpper(),
                    FontSize = 10,
                    TextColor = isToday ? AppColors.Primary : AppColors.TextSecondary,
                    HorizontalTextAlignment = TextAlignment.Center
                });

                var dayNumber = new Label
                {
                    Text = date.Day.ToString(),
                    FontSize = 16,
                    FontAttributes = isToday ? FontAttributes.Bold : FontAttributes.None,
                    TextColor = isToday ? AppColors.TextOnDark : (isPast ? AppColors.TextTertiary : AppColors.TextPrimary),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
                };

                if (isToday)
                {
                    var todayCircle = new Border
                    {
                        BackgroundColor = AppColors.Primary,
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
                        Stroke = Colors.Transparent,
                        WidthRequest = 32,
                        HeightRequest = 32,
                        Content = dayNumber,
                        HorizontalOptions = LayoutOptions.Center
                    };
                    dayStack.Children.Add(todayCircle);
                }
                else
                {
                    dayStack.Children.Add(dayNumber);
                }

                var jobsOnDay = jobCounts.TryGetValue(date, out var count) ? count : 0;
                if (jobsOnDay > 0)
                {
                    dayStack.Children.Add(new Border
                    {
                        BackgroundColor = jobsOnDay > 3 ? AppColors.Error : AppColors.Success,
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 4 },
                        Stroke = Colors.Transparent,
                        WidthRequest = 8,
                        HeightRequest = 8,
                        HorizontalOptions = LayoutOptions.Center
                    });
                }

                Grid.SetColumn(dayStack, i);
                WeekCalendarGrid.Children.Add(dayStack);
            }
        });
    }

    private async Task LoadToDoItemsAsync(DateTime weekEnd, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var toDoItems = new List<ToDoItem>();
        var overdueItems = new List<ToDoItem>();

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            
            var unsentEstimates = await db.Estimates
                .AsNoTracking()
                .Include(e => e.Customer)
                .Where(e => e.Status == EstimateStatus.Draft)
                .OrderBy(e => e.CreatedAt)
                .Take(5)
                .ToListAsync(cancellationToken);

            foreach (var est in unsentEstimates)
            {
                toDoItems.Add(new ToDoItem
                {
                    Title = $"Send estimate to {est.Customer?.Name ?? "Customer"}",
                    Subtitle = $"{est.Title} - ${est.Total:N0}",
                    Category = "Estimate",
                    Color = AppColors.Warning,
                    Action = async () => await Shell.Current.GoToAsync("//EstimatesTab")
                });
            }

            var unpaidInvoices = await db.Invoices
                .Include(i => i.Customer)
                .Where(i => i.Status == InvoiceStatus.Sent && i.BalanceDue > 0)
                .OrderBy(i => i.DueDate)
                .Take(5)
                .ToListAsync(cancellationToken);

            foreach (var inv in unpaidInvoices)
            {
                var isOverdue = inv.DueDate < today;
                var item = new ToDoItem
                {
                    Title = $"Collect payment from {inv.Customer?.Name ?? "Customer"}",
                    Subtitle = $"{inv.InvoiceNumber} - ${inv.BalanceDue:N0} due {inv.DueDate:MMM d}",
                    Category = isOverdue ? "Overdue" : "Invoice",
                    Color = isOverdue ? AppColors.Error : AppColors.Success,
                    Action = async () => await Shell.Current.GoToAsync("//InvoicesTab")
                };

                if (isOverdue)
                    overdueItems.Add(item);
                else
                    toDoItems.Add(item);
            }

            var upcomingJobs = await db.Jobs
                .Include(j => j.Customer)
                .Where(j => j.Status == JobStatus.Scheduled &&
                           j.ScheduledDate.HasValue &&
                           j.ScheduledDate.Value.Date >= today &&
                           j.ScheduledDate.Value.Date <= weekEnd)
                .OrderBy(j => j.ScheduledDate)
                .Take(5)
                .ToListAsync(cancellationToken);

            foreach (var job in upcomingJobs)
            {
                toDoItems.Add(new ToDoItem
                {
                    Title = job.Title,
                    Subtitle = $"{job.Customer?.Name} - {job.ScheduledDate:ddd, MMM d}",
                    Category = "Job",
                    Color = AppColors.Primary,
                    Action = async () => await Shell.Current.GoToAsync($"JobDetail?id={job.Id}")
                });
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch
        {
            // Show empty state
        }

        if (cancellationToken.IsCancellationRequested) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            ToDoList.Children.Clear();
            OverdueList.Children.Clear();
            
            foreach (var item in toDoItems.Take(8))
            {
                ToDoList.Children.Add(CreateToDoCard(item));
            }

            ToDoEmptyState.IsVisible = !toDoItems.Any() && !overdueItems.Any();

            OverdueHeader.IsVisible = overdueItems.Any();
            foreach (var item in overdueItems)
            {
                OverdueList.Children.Add(CreateToDoCard(item));
            }
        });
    }

    private Border CreateToDoCard(ToDoItem item)
    {
        var card = new Border
        {
            BackgroundColor = AppColors.Surface,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Stroke = Colors.Transparent,
            Padding = 12,
            Shadow = new Shadow
            {
                Brush = Colors.Black,
                Offset = new Point(0, 1),
                Radius = 2,
                Opacity = 0.1f
            }
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(new GridLength(4)),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12
        };

        var colorBar = new Border
        {
            BackgroundColor = item.Color,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 2 },
            Stroke = Colors.Transparent,
            WidthRequest = 4,
            VerticalOptions = LayoutOptions.Fill
        };
        Grid.SetColumn(colorBar, 0);
        grid.Children.Add(colorBar);

        var content = new VerticalStackLayout { Spacing = 2 };
        content.Children.Add(new Label
        {
            Text = item.Title,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = AppColors.TextPrimary,
            LineBreakMode = LineBreakMode.TailTruncation
        });
        content.Children.Add(new Label
        {
            Text = item.Subtitle,
            FontSize = 12,
            TextColor = AppColors.TextSecondary,
            LineBreakMode = LineBreakMode.TailTruncation
        });
        Grid.SetColumn(content, 1);
        grid.Children.Add(content);

        var badge = new Border
        {
            BackgroundColor = item.Color.WithAlpha(0.15f),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
            Stroke = Colors.Transparent,
            Padding = new Thickness(8, 4),
            VerticalOptions = LayoutOptions.Center
        };
        badge.Content = new Label
        {
            Text = item.Category,
            FontSize = 10,
            TextColor = item.Color,
            FontAttributes = FontAttributes.Bold
        };
        Grid.SetColumn(badge, 2);
        grid.Children.Add(badge);

        card.Content = grid;

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (s, e) =>
        {
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            if (item.Action != null)
                await item.Action();
        };
        card.GestureRecognizers.Add(tapGesture);

        return card;
    }

    #endregion

    #region Dashboard 3: Month Calendar View

    private async Task LoadMonthDashboardAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await LoadMonthJobCountsAsync(MonthCalendar.DisplayMonth, cancellationToken);
            MonthCalendar.JobCounts = _monthJobCounts;
            
            await LoadSelectedDayJobsAsync(_selectedCalendarDate, cancellationToken);
            await LoadMonthStatsAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                await DisplayAlertAsync("Error", $"Could not load month data: {ex.Message}", "OK");
            }
        }
    }

    private async Task LoadMonthJobCountsAsync(DateTime month, CancellationToken cancellationToken = default)
    {
        _monthJobCounts.Clear();
        
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        
        var firstOfMonth = new DateTime(month.Year, month.Month, 1);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
        
        var jobs = await db.Jobs
            .Where(j => j.ScheduledDate.HasValue &&
                       j.ScheduledDate.Value.Date >= firstOfMonth &&
                       j.ScheduledDate.Value.Date <= lastOfMonth &&
                       j.Status != JobStatus.Cancelled)
            .Select(j => j.ScheduledDate!.Value.Date)
            .ToListAsync(cancellationToken);
        
        foreach (var date in jobs)
        {
            if (_monthJobCounts.ContainsKey(date))
                _monthJobCounts[date]++;
            else
                _monthJobCounts[date] = 1;
        }
    }

    private async Task LoadSelectedDayJobsAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        
        _selectedDayJobs = await db.Jobs
            // .Include(j => j.Customer)
            // .Include(j => j.Site)
            .Where(j => j.ScheduledDate.HasValue &&
                       j.ScheduledDate.Value.Date == date &&
                       j.Status != JobStatus.Cancelled)
            .OrderBy(j => j.ScheduledDate)
            .ToListAsync(cancellationToken);

        if (!cancellationToken.IsCancellationRequested)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateSelectedDayDisplay(date);
                BuildSelectedDayJobsList();
            });
        }
    }

    private void UpdateSelectedDayDisplay(DateTime date)
    {
        if (date == DateTime.Today)
        {
            SelectedDayLabel.Text = "Today";
        }
        else if (date == DateTime.Today.AddDays(1))
        {
            SelectedDayLabel.Text = "Tomorrow";
        }
        else if (date == DateTime.Today.AddDays(-1))
        {
            SelectedDayLabel.Text = "Yesterday";
        }
        else
        {
            SelectedDayLabel.Text = date.ToString("dddd");
        }
        
        SelectedDayDateLabel.Text = date.ToString("MMMM d, yyyy");
    }

    private void BuildSelectedDayJobsList()
    {
        SelectedDayJobsList.Children.Clear();

        if (!_selectedDayJobs.Any())
        {
            SelectedDayEmptyState.IsVisible = true;
            return;
        }

        SelectedDayEmptyState.IsVisible = false;

        foreach (var job in _selectedDayJobs)
        {
            SelectedDayJobsList.Children.Add(CreateJobCard(job));
        }
    }

    private async Task LoadMonthStatsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        
        var month = MonthCalendar.DisplayMonth;
        var firstOfMonth = new DateTime(month.Year, month.Month, 1);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);

        var monthJobs = await db.Jobs
            .Where(j => j.ScheduledDate.HasValue &&
                       j.ScheduledDate.Value.Date >= firstOfMonth &&
                       j.ScheduledDate.Value.Date <= lastOfMonth)
            .ToListAsync(cancellationToken);

        var totalJobs = monthJobs.Count(j => j.Status != JobStatus.Cancelled);
        var completedJobs = monthJobs.Count(j => j.Status == JobStatus.Completed || j.Status == JobStatus.Closed);
        var revenue = monthJobs
            .Where(j => j.Status == JobStatus.Completed || j.Status == JobStatus.Closed)
            .Sum(j => j.Total);

        if (!cancellationToken.IsCancellationRequested)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MonthJobsLabel.Text = totalJobs.ToString();
                MonthCompletedLabel.Text = completedJobs.ToString();
                MonthRevenueLabel.Text = revenue >= 1000 
                    ? $"${revenue / 1000:F1}k" 
                    : $"${revenue:F0}";
            });
        }
    }

    private async void OnMonthCalendarDayTapped(object? sender, DateTime date)
    {
        _selectedCalendarDate = date;
        
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        
        await LoadSelectedDayJobsAsync(date, _cts.Token);
    }

    private async void OnMonthCalendarMonthChanged(object? sender, DateTime newMonth)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        
        await LoadMonthJobCountsAsync(newMonth, _cts.Token);
        MonthCalendar.JobCounts = _monthJobCounts;
        await LoadMonthStatsAsync(_cts.Token);
    }

    private void OnQuickScheduleClicked(object? sender, EventArgs e)
    {
        QuickSchedulePopup.Show(_selectedCalendarDate, _selectedDayJobs);
    }

    private async void OnQuickScheduleNewJob(object? sender, ScheduleEventArgs e)
    {
        QuickSchedulePopup.Hide();
        
        // Navigate to add job page with pre-filled date
        var dateParam = e.ScheduledDateTime?.ToString("o") ?? e.Date.ToString("o");
        await Shell.Current.GoToAsync($"AddJob?scheduledDate={Uri.EscapeDataString(dateParam)}");
    }

    private async void OnQuickScheduleNewEstimate(object? sender, ScheduleEventArgs e)
    {
        QuickSchedulePopup.Hide();
        
        // Navigate to add estimate page with pre-filled date
        var dateParam = e.ScheduledDateTime?.ToString("o") ?? e.Date.ToString("o");
        await Shell.Current.GoToAsync($"AddEstimate?scheduledDate={Uri.EscapeDataString(dateParam)}");
    }

    private async void OnQuickScheduleReschedule(object? sender, ScheduleEventArgs e)
    {
        QuickSchedulePopup.Hide();
        
        // Show job list for selecting a job to reschedule
        await Shell.Current.GoToAsync("//JobsTab");
    }

    private void OnQuickSchedulePopupClosed(object? sender, EventArgs e)
    {
        QuickSchedulePopup.IsVisible = false;
    }

    #endregion

    #region Quick Actions

    private async void OnNewEstimateClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AddEstimate");
    }

    private async void OnAddCustomerClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("CustomerDetail");
    }

    private async void OnSearchAssetsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Assets");
    }

    #endregion
}

/// <summary>
/// Represents a to-do item on Dashboard 2.
/// </summary>
internal class ToDoItem
{
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string Category { get; set; } = "";
    public Color Color { get; set; } = Colors.Gray;
    public Func<Task>? Action { get; set; }
}
