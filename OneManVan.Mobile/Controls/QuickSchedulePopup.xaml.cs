using OneManVan.Shared.Models;

namespace OneManVan.Mobile.Controls;

/// <summary>
/// Quick schedule popup that appears when tapping a day on the calendar.
/// Allows quick job/estimate creation or rescheduling.
/// </summary>
public partial class QuickSchedulePopup : ContentView
{
    private DateTime _selectedDate;
    private TimeSpan? _selectedTime;
    private List<Job>? _existingJobs;

    /// <summary>
    /// The date that was selected for scheduling.
    /// </summary>
    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            _selectedDate = value;
            UpdateDateDisplay();
        }
    }

    /// <summary>
    /// The time slot selected (if any).
    /// </summary>
    public TimeSpan? SelectedTime
    {
        get => _selectedTime;
        set => _selectedTime = value;
    }

    /// <summary>
    /// Existing jobs on the selected date.
    /// </summary>
    public List<Job>? ExistingJobs
    {
        get => _existingJobs;
        set
        {
            _existingJobs = value;
            UpdateExistingJobsDisplay();
        }
    }

    #region Events

    /// <summary>Fired when user wants to create a new job.</summary>
    public event EventHandler<ScheduleEventArgs>? NewJobRequested;

    /// <summary>Fired when user wants to create a new estimate.</summary>
    public event EventHandler<ScheduleEventArgs>? NewEstimateRequested;

    /// <summary>Fired when user wants to reschedule an existing job.</summary>
    public event EventHandler<ScheduleEventArgs>? RescheduleRequested;

    /// <summary>Fired when user wants to block time.</summary>
    public event EventHandler<ScheduleEventArgs>? BlockTimeRequested;

    /// <summary>Fired when popup is closed.</summary>
    public event EventHandler? Closed;

    #endregion

    public QuickSchedulePopup()
    {
        InitializeComponent();
        _selectedDate = DateTime.Today;
        UpdateDateDisplay();
        BuildTimeSlots();
    }

    private void UpdateDateDisplay()
    {
        DateLabel.Text = _selectedDate.ToString("MMMM d, yyyy");
        DayOfWeekLabel.Text = _selectedDate.ToString("dddd");

        // Show different messaging for past dates
        if (_selectedDate < DateTime.Today)
        {
            DayOfWeekLabel.Text = $"{_selectedDate:dddd} (Past)";
        }
        else if (_selectedDate == DateTime.Today)
        {
            DayOfWeekLabel.Text = "Today";
        }
    }

    private void UpdateExistingJobsDisplay()
    {
        ExistingJobsList.Children.Clear();

        if (_existingJobs == null || _existingJobs.Count == 0)
        {
            ExistingJobsSection.IsVisible = false;
            return;
        }

        ExistingJobsSection.IsVisible = true;

        foreach (var job in _existingJobs.Take(3))
        {
            var jobCard = new Border
            {
                BackgroundColor = Color.FromArgb("#E3F2FD"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
                Stroke = Colors.Transparent,
                Padding = new Thickness(10, 8)
            };

            var content = new HorizontalStackLayout { Spacing = 8 };

            // Time
            var timeText = job.ArrivalWindowStart?.ToString("h:mm tt") ?? "TBD";
            content.Children.Add(new Label
            {
                Text = timeText,
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1976D2"),
                VerticalOptions = LayoutOptions.Center
            });

            // Title
            content.Children.Add(new Label
            {
                Text = job.Title ?? "Job",
                FontSize = 12,
                TextColor = Color.FromArgb("#333333"),
                LineBreakMode = LineBreakMode.TailTruncation,
                VerticalOptions = LayoutOptions.Center
            });

            jobCard.Content = content;
            ExistingJobsList.Children.Add(jobCard);
        }

        if (_existingJobs.Count > 3)
        {
            ExistingJobsList.Children.Add(new Label
            {
                Text = $"+{_existingJobs.Count - 3} more jobs",
                FontSize = 11,
                TextColor = Color.FromArgb("#757575"),
                Margin = new Thickness(0, 4, 0, 0)
            });
        }
    }

    private void BuildTimeSlots()
    {
        TimeSlotButtons.Children.Clear();

        var timeSlots = new[]
        {
            ("8:00 AM", new TimeSpan(8, 0, 0)),
            ("9:00 AM", new TimeSpan(9, 0, 0)),
            ("10:00 AM", new TimeSpan(10, 0, 0)),
            ("11:00 AM", new TimeSpan(11, 0, 0)),
            ("12:00 PM", new TimeSpan(12, 0, 0)),
            ("1:00 PM", new TimeSpan(13, 0, 0)),
            ("2:00 PM", new TimeSpan(14, 0, 0)),
            ("3:00 PM", new TimeSpan(15, 0, 0)),
            ("4:00 PM", new TimeSpan(16, 0, 0)),
            ("5:00 PM", new TimeSpan(17, 0, 0))
        };

        foreach (var (label, time) in timeSlots)
        {
            var button = new Button
            {
                Text = label,
                FontSize = 11,
                HeightRequest = 32,
                Padding = new Thickness(8, 0),
                Margin = new Thickness(0, 0, 8, 8),
                CornerRadius = 16
            };
            button.SetAppThemeColor(Button.BackgroundColorProperty, Color.FromArgb("#F5F5F5"), Color.FromArgb("#424242"));
            button.SetAppThemeColor(Button.TextColorProperty, Color.FromArgb("#333333"), Colors.White);

            button.Clicked += (s, e) => OnTimeSlotSelected(time, button);
            TimeSlotButtons.Children.Add(button);
        }
    }

    private void OnTimeSlotSelected(TimeSpan time, Button button)
    {
        _selectedTime = time;

        // Reset all button styles
        foreach (var child in TimeSlotButtons.Children)
        {
            if (child is Button btn)
            {
                btn.SetAppThemeColor(Button.BackgroundColorProperty, Color.FromArgb("#F5F5F5"), Color.FromArgb("#424242"));
                btn.SetAppThemeColor(Button.TextColorProperty, Color.FromArgb("#333333"), Colors.White);
            }
        }

        // Highlight selected
        button.BackgroundColor = Color.FromArgb("#2196F3");
        button.TextColor = Colors.White;

        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }

    #region Event Handlers

    private void OnCloseClicked(object? sender, EventArgs e)
    {
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private void OnNewJobClicked(object? sender, EventArgs e)
    {
        // Show time slots first if not selected
        if (_selectedTime == null)
        {
            TimeSlotSection.IsVisible = true;
            return;
        }

        NewJobRequested?.Invoke(this, new ScheduleEventArgs(_selectedDate, _selectedTime));
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }

    private void OnNewEstimateClicked(object? sender, EventArgs e)
    {
        if (_selectedTime == null)
        {
            TimeSlotSection.IsVisible = true;
            return;
        }

        NewEstimateRequested?.Invoke(this, new ScheduleEventArgs(_selectedDate, _selectedTime));
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }

    private void OnRescheduleClicked(object? sender, EventArgs e)
    {
        RescheduleRequested?.Invoke(this, new ScheduleEventArgs(_selectedDate, _selectedTime));
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }

    private void OnBlockTimeClicked(object? sender, EventArgs e)
    {
        if (_selectedTime == null)
        {
            TimeSlotSection.IsVisible = true;
            return;
        }

        BlockTimeRequested?.Invoke(this, new ScheduleEventArgs(_selectedDate, _selectedTime));
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Shows the popup for the specified date.
    /// </summary>
    public void Show(DateTime date, List<Job>? existingJobs = null)
    {
        SelectedDate = date;
        ExistingJobs = existingJobs;
        _selectedTime = null;
        TimeSlotSection.IsVisible = false;
        IsVisible = true;
    }

    /// <summary>
    /// Hides the popup.
    /// </summary>
    public void Hide()
    {
        IsVisible = false;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}

/// <summary>
/// Event args for schedule-related events.
/// </summary>
public class ScheduleEventArgs : EventArgs
{
    public DateTime Date { get; }
    public TimeSpan? Time { get; }
    public DateTime? ScheduledDateTime => Time.HasValue ? Date.Add(Time.Value) : null;

    public ScheduleEventArgs(DateTime date, TimeSpan? time = null)
    {
        Date = date;
        Time = time;
    }
}
