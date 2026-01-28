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
/// Calendar view for job scheduling with month/week views.
/// </summary>
public partial class CalendarSchedulingPage : UserControl
{
    private readonly OneManVanDbContext _dbContext;
    private DateTime _currentDate;
    private List<Job> _monthJobs = [];
    private List<Job> _unscheduledJobs = [];
    private bool _isInitialLoad = true;

    public CalendarSchedulingPage(OneManVanDbContext dbContext)
    {
        InitializeComponent();
        _dbContext = dbContext;
        _currentDate = DateTime.Today;
        Loaded += OnPageLoaded;
    }

    // Simple relay command for double-click
    private class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute) => _execute = execute;
#pragma warning disable CS0067 // Event never used but required by ICommand interface
        public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        await LoadCalendarAsync();
    }

    private async Task LoadCalendarAsync()
    {
        try
        {
            // Show appropriate loading state
            if (_isInitialLoad)
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                if (CalendarGrid != null) CalendarGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Refreshing - disable refresh button
                if (RefreshButton != null)
                {
                    RefreshButton.IsEnabled = false;
                    RefreshButton.Content = "Refreshing...";
                    RefreshButton.Opacity = 0.6;
                }
            }

            var firstOfMonth = new DateTime(_currentDate.Year, _currentDate.Month, 1);
            var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);

            // Load jobs for current month - fix SQL translation issue
            _monthJobs = await _dbContext.Jobs
                .Include(j => j.Customer)
                .Where(j => j.ScheduledDate.HasValue && 
                           j.ScheduledDate.Value >= firstOfMonth && 
                           j.ScheduledDate.Value <= lastOfMonth)
                .Where(j => j.Status != JobStatus.Cancelled)
                .ToListAsync();

            // Sort in memory after retrieval
            _monthJobs = _monthJobs
                .OrderBy(j => j.ScheduledDate)
                .ThenBy(j => j.ArrivalWindowStart)
                .ToList();

            // Load unscheduled jobs
            _unscheduledJobs = await _dbContext.Jobs
                .Include(j => j.Customer)
                .Where(j => !j.ScheduledDate.HasValue || j.Status == JobStatus.Draft)
                .Where(j => j.Status != JobStatus.Cancelled && j.Status != JobStatus.Closed)
                .ToListAsync();

            // Sort in memory
            _unscheduledJobs = _unscheduledJobs
                .OrderBy(j => j.Priority)
                .ThenBy(j => j.CreatedAt)
                .ToList();

            // Update UI
            CurrentMonthText.Text = _currentDate.ToString("MMMM yyyy");
            MonthJobCount.Text = $"{_monthJobs.Count} jobs";
            UnscheduledCount.Text = _unscheduledJobs.Count.ToString();

            // Show/hide unscheduled panel
            UnscheduledPanel.Visibility = _unscheduledJobs.Any() ? Visibility.Visible : Visibility.Collapsed;

            BuildCalendarGrid();
            PopulateUnscheduledJobs();
            
            if (CalendarGrid != null) CalendarGrid.Visibility = Visibility.Visible;
            _isInitialLoad = false;
        }
        catch (Exception ex)
        {
            ToastService.Error($"Failed to load calendar: {ex.Message}");
            
            if (_isInitialLoad)
            {
                ShowErrorState("Failed to load calendar data. Please try again.");
            }
        }
        finally
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            
            // Reset refresh button
            if (RefreshButton != null)
            {
                RefreshButton.IsEnabled = true;
                RefreshButton.Content = "Refresh";
                RefreshButton.Opacity = 1.0;
            }
        }
    }

    private void ShowErrorState(string message)
    {
        if (CalendarGrid == null) return;
        
        CalendarGrid.Children.Clear();
        CalendarGrid.RowDefinitions.Clear();
        CalendarGrid.ColumnDefinitions.Clear();
        CalendarGrid.Visibility = Visibility.Visible;
        
        var errorPanel = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        
        errorPanel.Children.Add(new TextBlock
        {
            Text = "⚠",
            FontSize = 48,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10),
            Foreground = (Brush)FindResource("TextBrush")
        });
        
        errorPanel.Children.Add(new TextBlock
        {
            Text = message,
            FontSize = 14,
            Foreground = (Brush)FindResource("SubtextBrush"),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 400
        });
        
        var retryButton = new Button
        {
            Content = "Retry",
            Margin = new Thickness(0, 20, 0, 0),
            Padding = new Thickness(20, 8, 20, 8),
            Background = (Brush)FindResource("PrimaryBrush"),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        retryButton.Click += async (s, e) => { _isInitialLoad = true; await LoadCalendarAsync(); };
        errorPanel.Children.Add(retryButton);
        
        CalendarGrid.Children.Add(errorPanel);
    }

    private void BuildCalendarGrid()
    {
        CalendarGrid.Children.Clear();
        CalendarGrid.RowDefinitions.Clear();
        CalendarGrid.ColumnDefinitions.Clear();

        // Setup 7 columns for days of week
        for (int i = 0; i < 7; i++)
        {
            CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        var firstOfMonth = new DateTime(_currentDate.Year, _currentDate.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(_currentDate.Year, _currentDate.Month);
        var startDayOfWeek = (int)firstOfMonth.DayOfWeek;
        var totalCells = startDayOfWeek + daysInMonth;
        var rows = (int)Math.Ceiling(totalCells / 7.0);

        // Add rows
        for (int i = 0; i < rows; i++)
        {
            CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 120 });
        }

        // Fill calendar cells
        var currentDay = 1;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                var cellIndex = row * 7 + col;
                var isValidDay = cellIndex >= startDayOfWeek && currentDay <= daysInMonth;
                var cellDate = isValidDay ? new DateTime(_currentDate.Year, _currentDate.Month, currentDay) : (DateTime?)null;

                var cell = CreateDayCell(cellDate, isValidDay ? currentDay : 0);
                Grid.SetRow(cell, row);
                Grid.SetColumn(cell, col);
                CalendarGrid.Children.Add(cell);

                if (isValidDay)
                {
                    // Add jobs for this day
                    var dayJobs = _monthJobs.Where(j => j.ScheduledDate?.Date == cellDate?.Date).ToList();
                    PopulateDayCell(cell, dayJobs);
                    currentDay++;
                }
            }
        }
    }

    private Border CreateDayCell(DateTime? date, int dayNumber)
    {
        var isToday = date?.Date == DateTime.Today;
        var isPast = date?.Date < DateTime.Today;
        var isWeekend = date?.DayOfWeek == DayOfWeek.Saturday || date?.DayOfWeek == DayOfWeek.Sunday;

        var cell = new Border
        {
            Background = isToday 
                ? new SolidColorBrush(Color.FromArgb(30, 33, 150, 243))
                : isWeekend 
                    ? (Brush)FindResource("Surface2Brush")
                    : (Brush)FindResource("SurfaceBrush"),
            BorderBrush = (Brush)FindResource("Surface2Brush"),
            BorderThickness = new Thickness(0, 0, 1, 1),
            Padding = new Thickness(6),
            AllowDrop = date.HasValue,
            Tag = date
        };

        if (date.HasValue)
        {
            cell.Drop += OnDayDrop;
            cell.DragOver += OnDayDragOver;
        }

        var content = new Grid();
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // Day number header
        if (dayNumber > 0)
        {
            var dayHeader = new TextBlock
            {
                Text = dayNumber.ToString(),
                FontSize = 14,
                FontWeight = isToday ? FontWeights.Bold : FontWeights.Normal,
                Foreground = isToday 
                    ? (Brush)FindResource("PrimaryBrush") 
                    : isPast 
                        ? (Brush)FindResource("SubtextBrush")
                        : (Brush)FindResource("TextBrush"),
                Margin = new Thickness(2, 0, 0, 4)
            };

            if (isToday)
            {
                var todayBadge = new Border
                {
                    Background = (Brush)FindResource("PrimaryBrush"),
                    CornerRadius = new CornerRadius(10),
                    Width = 24,
                    Height = 24,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                todayBadge.Child = new TextBlock
                {
                    Text = dayNumber.ToString(),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(todayBadge, 0);
                content.Children.Add(todayBadge);
            }
            else
            {
                Grid.SetRow(dayHeader, 0);
                content.Children.Add(dayHeader);
            }
        }

        // Jobs container
        var jobsContainer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        var jobsStack = new StackPanel { Tag = "JobsContainer" };
        jobsContainer.Content = jobsStack;
        Grid.SetRow(jobsContainer, 1);
        content.Children.Add(jobsContainer);

        cell.Child = content;
        return cell;
    }

    private void PopulateDayCell(Border cell, List<Job> jobs)
    {
        var content = cell.Child as Grid;
        var scrollViewer = content?.Children.OfType<ScrollViewer>().FirstOrDefault();
        var jobsStack = scrollViewer?.Content as StackPanel;

        if (jobsStack == null) return;

        foreach (var job in jobs.Take(5)) // Show max 5 jobs per cell
        {
            var jobItem = CreateJobItem(job);
            jobsStack.Children.Add(jobItem);
        }

        if (jobs.Count > 5)
        {
            var moreText = new TextBlock
            {
                Text = $"+{jobs.Count - 5} more",
                FontSize = 10,
                Foreground = (Brush)FindResource("SubtextBrush"),
                Margin = new Thickness(4, 2, 0, 0)
            };
            jobsStack.Children.Add(moreText);
        }
    }

    private Border CreateJobItem(Job job)
    {
        var color = GetJobStatusColor(job.Status);
        
        var item = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(200, color.R, color.G, color.B)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 3, 6, 3),
            Margin = new Thickness(0, 0, 0, 2),
            Cursor = Cursors.Hand,
            Tag = job
        };

        // Enable drag
        item.MouseLeftButtonDown += OnJobItemMouseDown;
        item.MouseMove += OnJobItemMouseMove;
        
        // Add double-click via InputBinding
        item.InputBindings.Add(new MouseBinding(
            new RelayCommand(() => OnJobItemDoubleClickCommand(job)),
            new MouseGesture(MouseAction.LeftDoubleClick)));

        var content = new StackPanel();

        // Time (if available)
        if (job.ArrivalWindowStart.HasValue)
        {
            var time = new TextBlock
            {
                Text = job.ArrivalWindowStart.Value.ToString("h:mm tt"),
                FontSize = 9,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold
            };
            content.Children.Add(time);
        }

        // Title
        var title = new TextBlock
        {
            Text = job.Title ?? "Job",
            FontSize = 11,
            Foreground = Brushes.White,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 150
        };
        content.Children.Add(title);

        // Customer
        if (job.Customer != null)
        {
            var customer = new TextBlock
            {
                Text = job.Customer.Name,
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            content.Children.Add(customer);
        }

        item.Child = content;
        return item;
    }

    private static Color GetJobStatusColor(JobStatus status) => status switch
    {
        JobStatus.Draft => Color.FromRgb(158, 158, 158),
        JobStatus.Scheduled => Color.FromRgb(33, 150, 243),
        JobStatus.EnRoute => Color.FromRgb(255, 152, 0),
        JobStatus.InProgress => Color.FromRgb(76, 175, 80),
        JobStatus.Completed => Color.FromRgb(103, 58, 183),
        JobStatus.OnHold => Color.FromRgb(244, 67, 54),
        _ => Color.FromRgb(158, 158, 158)
    };

    private void PopulateUnscheduledJobs()
    {
        UnscheduledJobsList.Items.Clear();

        foreach (var job in _unscheduledJobs)
        {
            var item = CreateUnscheduledJobItem(job);
            UnscheduledJobsList.Items.Add(item);
        }

        if (!_unscheduledJobs.Any())
        {
            var emptyText = new TextBlock
            {
                Text = "No unscheduled jobs",
                Foreground = (Brush)FindResource("SubtextBrush"),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };
            UnscheduledJobsList.Items.Add(emptyText);
        }
    }

    private Border CreateUnscheduledJobItem(Job job)
    {
        var item = new Border
        {
            Background = (Brush)FindResource("Surface2Brush"),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 0, 6),
            Cursor = Cursors.Hand,
            Tag = job
        };

        item.MouseLeftButtonDown += OnJobItemMouseDown;
        item.MouseMove += OnJobItemMouseMove;

        var content = new StackPanel();

        // Priority badge
        var priorityEmoji = job.Priority switch
        {
            JobPriority.Low => "",
            JobPriority.Normal => "",
            JobPriority.High => "",
            JobPriority.Urgent => "",
            JobPriority.Emergency => "",
            _ => "?"
        };

        var header = new StackPanel { Orientation = Orientation.Horizontal };
        header.Children.Add(new TextBlock { Text = priorityEmoji, FontSize = 10, Margin = new Thickness(0, 0, 4, 0) });
        header.Children.Add(new TextBlock
        {
            Text = job.JobNumber ?? $"#{job.Id}",
            FontSize = 10,
            Foreground = (Brush)FindResource("SubtextBrush")
        });
        content.Children.Add(header);

        // Title
        content.Children.Add(new TextBlock
        {
            Text = job.Title ?? "Untitled",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("TextBrush"),
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        // Customer
        if (job.Customer != null)
        {
            content.Children.Add(new TextBlock
            {
                Text = job.Customer.Name,
                FontSize = 11,
                Foreground = (Brush)FindResource("SubtextBrush"),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
        }

        item.Child = content;
        return item;
    }

    #region Drag and Drop

    private Point _dragStartPoint;
    private Job? _draggedJob;

    private void OnJobItemMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private void OnJobItemMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        var mousePos = e.GetPosition(null);
        var diff = _dragStartPoint - mousePos;

        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            if (sender is Border item && item.Tag is Job job)
            {
                _draggedJob = job;
                var data = new DataObject("CalendarJob", job);
                DragDrop.DoDragDrop(item, data, DragDropEffects.Move);
            }
        }
    }

    private void OnJobItemDoubleClickCommand(Job job)
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
                    await LoadCalendarAsync();
                    
                    ToastService.Success("Job updated!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to update job: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        );
    }

    private void OnDayDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("CalendarJob"))
        {
            e.Effects = DragDropEffects.None;
            return;
        }
        e.Effects = DragDropEffects.Move;
    }

    private async void OnDayDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("CalendarJob") || _draggedJob == null)
            return;

        if (sender is Border cell && cell.Tag is DateTime targetDate)
        {
            var oldDate = _draggedJob.ScheduledDate;
            _draggedJob.ScheduledDate = targetDate;

            if (_draggedJob.Status == JobStatus.Draft)
            {
                _draggedJob.Status = JobStatus.Scheduled;
            }

            try
            {
                await _dbContext.SaveChangesAsync();
                ToastService.Success($"Job scheduled for {targetDate:MMM d}");
                await LoadCalendarAsync();
            }
            catch (Exception ex)
            {
                _draggedJob.ScheduledDate = oldDate;
                ToastService.Error($"Failed to reschedule job: {ex.Message}");
            }
            finally
            {
                _draggedJob = null;
            }
        }
    }

    #endregion

    #region Event Handlers

    private void OnPrevMonthClick(object sender, RoutedEventArgs e)
    {
        if (WeekViewRadio?.IsChecked == true)
        {
            _currentDate = _currentDate.AddDays(-7);
            BuildWeekView();
        }
        else
        {
            _currentDate = _currentDate.AddMonths(-1);
            _ = LoadCalendarAsync();
        }
    }

    private void OnNextMonthClick(object sender, RoutedEventArgs e)
    {
        if (WeekViewRadio?.IsChecked == true)
        {
            _currentDate = _currentDate.AddDays(7);
            BuildWeekView();
        }
        else
        {
            _currentDate = _currentDate.AddMonths(1);
            _ = LoadCalendarAsync();
        }
    }

    private void OnTodayClick(object sender, RoutedEventArgs e)
    {
        _currentDate = DateTime.Today;
        
        if (WeekViewRadio?.IsChecked == true)
        {
            BuildWeekView();
        }
        else
        {
            _ = LoadCalendarAsync();
        }
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await LoadCalendarAsync();
        ToastService.Success("Calendar refreshed");
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
                    await LoadCalendarAsync();
                    
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

    private void OnViewChanged(object sender, RoutedEventArgs e)
    {
        // Guard: Don't execute if controls aren't initialized yet (during InitializeComponent)
        if (LoadingOverlay == null || CalendarGrid == null)
            return;

        if (sender == WeekViewRadio && WeekViewRadio?.IsChecked == true)
        {
            BuildWeekView();
        }
        else if (sender == MonthViewRadio && MonthViewRadio?.IsChecked == true)
        {
            _ = LoadCalendarAsync();
        }
    }

    private void BuildWeekView()
    {
        CalendarGrid.Children.Clear();
        CalendarGrid.RowDefinitions.Clear();
        CalendarGrid.ColumnDefinitions.Clear();

        // Get start of week (Sunday)
        var startOfWeek = _currentDate.AddDays(-(int)_currentDate.DayOfWeek);
        
        // Update header to show week range
        var endOfWeek = startOfWeek.AddDays(6);
        CurrentMonthText.Text = $"{startOfWeek:MMM d} - {endOfWeek:MMM d, yyyy}";

        // Setup 8 columns: Time labels + 7 days
        CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
        for (int i = 0; i < 7; i++)
        {
            CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        // Setup rows: Header + 24 hours
        CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (int hour = 0; hour < 24; hour++)
        {
            CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });
        }

        // Day headers
        for (int day = 0; day < 7; day++)
        {
            var date = startOfWeek.AddDays(day);
            var isToday = date.Date == DateTime.Today;

            var header = new Border
            {
                Background = isToday 
                    ? new SolidColorBrush(Color.FromArgb(50, 33, 150, 243))
                    : (Brush)FindResource("Surface2Brush"),
                BorderBrush = (Brush)FindResource("Surface2Brush"),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Padding = new Thickness(8)
            };

            var headerContent = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            
            headerContent.Children.Add(new TextBlock
            {
                Text = date.DayOfWeek.ToString().Substring(0, 3),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("SubtextBrush"),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            headerContent.Children.Add(new TextBlock
            {
                Text = date.Day.ToString(),
                FontSize = 18,
                FontWeight = isToday ? FontWeights.Bold : FontWeights.Normal,
                Foreground = isToday 
                    ? (Brush)FindResource("PrimaryBrush")
                    : (Brush)FindResource("TextBrush"),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            header.Child = headerContent;
            Grid.SetRow(header, 0);
            Grid.SetColumn(header, day + 1);
            CalendarGrid.Children.Add(header);
        }

        // Time labels and day cells
        for (int hour = 0; hour < 24; hour++)
        {
            // Time label
            var timeLabel = new Border
            {
                Background = (Brush)FindResource("Surface2Brush"),
                BorderBrush = (Brush)FindResource("Surface2Brush"),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Padding = new Thickness(4)
            };

            timeLabel.Child = new TextBlock
            {
                Text = new DateTime(2000, 1, 1, hour, 0, 0).ToString("h tt"),
                FontSize = 10,
                Foreground = (Brush)FindResource("SubtextBrush"),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top
            };

            Grid.SetRow(timeLabel, hour + 1);
            Grid.SetColumn(timeLabel, 0);
            CalendarGrid.Children.Add(timeLabel);

            // Day cells
            for (int day = 0; day < 7; day++)
            {
                var date = startOfWeek.AddDays(day);
                var isToday = date.Date == DateTime.Today;

                var cell = new Border
                {
                    Background = isToday 
                        ? new SolidColorBrush(Color.FromArgb(10, 33, 150, 243))
                        : (Brush)FindResource("SurfaceBrush"),
                    BorderBrush = (Brush)FindResource("Surface2Brush"),
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    AllowDrop = true,
                    Tag = new { Date = date, Hour = hour }
                };

                cell.Drop += OnWeekCellDrop;
                cell.DragOver += OnDayDragOver;

                Grid.SetRow(cell, hour + 1);
                Grid.SetColumn(cell, day + 1);
                CalendarGrid.Children.Add(cell);
            }
        }

        // Populate with jobs - load from database if needed
        _ = PopulateWeekJobsAsync(startOfWeek);
    }

    private async Task PopulateWeekJobsAsync(DateTime startOfWeek)
    {
        var endOfWeek = startOfWeek.AddDays(7);

        try
        {
            // Load jobs for the week directly from database
            var weekJobs = await _dbContext.Jobs
                .Include(j => j.Customer)
                .Where(j => j.ScheduledDate.HasValue &&
                           j.ScheduledDate.Value >= startOfWeek &&
                           j.ScheduledDate.Value < endOfWeek)
                .Where(j => j.Status != JobStatus.Cancelled)
                .ToListAsync();

            foreach (var job in weekJobs)
            {
                if (!job.ScheduledDate.HasValue) continue;

                var dayOfWeek = (int)(job.ScheduledDate.Value.DayOfWeek);
                var hour = job.ArrivalWindowStart.HasValue 
                    ? (int)job.ArrivalWindowStart.Value.TotalHours 
                    : 9; // Default to 9 AM

                var duration = job.EstimatedHours > 0 ? (int)job.EstimatedHours : 1;

                // Create job item
                var jobItem = CreateWeekJobItem(job);
                
                Grid.SetColumn(jobItem, dayOfWeek + 1);
                Grid.SetRow(jobItem, hour + 1);
                Grid.SetRowSpan(jobItem, Math.Max(1, duration));
                
                CalendarGrid.Children.Add(jobItem);
            }

            // Update counts
            MonthJobCount.Text = $"{weekJobs.Count} jobs";
        }
        catch (Exception ex)
        {
            ToastService.Error($"Failed to load week jobs: {ex.Message}");
        }
    }

    private Border CreateWeekJobItem(Job job)
    {
        var color = GetJobStatusColor(job.Status);

        var item = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(220, color.R, color.G, color.B)),
            BorderBrush = new SolidColorBrush(color),
            BorderThickness = new Thickness(2, 0, 0, 0),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(6, 4, 6, 4),
            Margin = new Thickness(2),
            Cursor = Cursors.Hand,
            Tag = job
        };

        // Enable drag
        item.MouseLeftButtonDown += OnJobItemMouseDown;
        item.MouseMove += OnJobItemMouseMove;
        
        // Add double-click
        item.InputBindings.Add(new MouseBinding(
            new RelayCommand(() => OnJobItemDoubleClickCommand(job)),
            new MouseGesture(MouseAction.LeftDoubleClick)));

        var content = new StackPanel();

        // Time
        if (job.ArrivalWindowStart.HasValue)
        {
            content.Children.Add(new TextBlock
            {
                Text = job.ArrivalWindowStart.Value.ToString("h:mm tt"),
                FontSize = 10,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold
            });
        }

        // Title
        content.Children.Add(new TextBlock
        {
            Text = job.Title ?? "Job",
            FontSize = 11,
            Foreground = Brushes.White,
            TextTrimming = TextTrimming.CharacterEllipsis,
            FontWeight = FontWeights.SemiBold
        });

        // Customer
        if (job.Customer != null)
        {
            content.Children.Add(new TextBlock
            {
                Text = job.Customer.Name,
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
        }

        item.Child = content;
        return item;
    }

    private async void OnWeekCellDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("CalendarJob") || _draggedJob == null)
            return;

        if (sender is Border cell && cell.Tag is object cellData)
        {
            var date = ((dynamic)cellData).Date as DateTime?;
            var hour = ((dynamic)cellData).Hour as int?;

            if (!date.HasValue || !hour.HasValue)
                return;

            var oldDate = _draggedJob.ScheduledDate;
            var oldTime = _draggedJob.ArrivalWindowStart;

            _draggedJob.ScheduledDate = date.Value;
            _draggedJob.ArrivalWindowStart = TimeSpan.FromHours(hour.Value);
            _draggedJob.ArrivalWindowEnd = TimeSpan.FromHours(hour.Value + 1);

            if (_draggedJob.Status == JobStatus.Draft)
            {
                _draggedJob.Status = JobStatus.Scheduled;
            }

            try
            {
                await _dbContext.SaveChangesAsync();
                ToastService.Success($"Job rescheduled to {date.Value:MMM d} at {hour.Value:h tt}");
                
                if (WeekViewRadio?.IsChecked == true)
                {
                    BuildWeekView();
                }
                else
                {
                    await LoadCalendarAsync();
                }
            }
            catch (Exception ex)
            {
                _draggedJob.ScheduledDate = oldDate;
                _draggedJob.ArrivalWindowStart = oldTime;
                ToastService.Error($"Failed to reschedule job: {ex.Message}");
            }
            finally
            {
                _draggedJob = null;
            }
        }
    }

    #endregion
}
