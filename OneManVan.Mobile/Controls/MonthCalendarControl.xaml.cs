using OneManVan.Mobile.Theme;

namespace OneManVan.Mobile.Controls;

/// <summary>
/// Reusable monthly calendar control with day selection and job indicators.
/// Supports tap-to-add-schedule functionality.
/// </summary>
public partial class MonthCalendarControl : ContentView
{
    #region Bindable Properties

    public static readonly BindableProperty SelectedDateProperty = BindableProperty.Create(
        nameof(SelectedDate),
        typeof(DateTime),
        typeof(MonthCalendarControl),
        DateTime.Today,
        propertyChanged: OnSelectedDateChanged);

    public static readonly BindableProperty DisplayMonthProperty = BindableProperty.Create(
        nameof(DisplayMonth),
        typeof(DateTime),
        typeof(MonthCalendarControl),
        new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
        propertyChanged: OnDisplayMonthChanged);

    public static readonly BindableProperty JobCountsProperty = BindableProperty.Create(
        nameof(JobCounts),
        typeof(Dictionary<DateTime, int>),
        typeof(MonthCalendarControl),
        null,
        propertyChanged: OnJobCountsChanged);

    public static readonly BindableProperty AllowNavigationProperty = BindableProperty.Create(
        nameof(AllowNavigation),
        typeof(bool),
        typeof(MonthCalendarControl),
        true,
        propertyChanged: OnAllowNavigationChanged);

    public DateTime SelectedDate
    {
        get => (DateTime)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public DateTime DisplayMonth
    {
        get => (DateTime)GetValue(DisplayMonthProperty);
        set => SetValue(DisplayMonthProperty, value);
    }

    public Dictionary<DateTime, int>? JobCounts
    {
        get => (Dictionary<DateTime, int>?)GetValue(JobCountsProperty);
        set => SetValue(JobCountsProperty, value);
    }

    public bool AllowNavigation
    {
        get => (bool)GetValue(AllowNavigationProperty);
        set => SetValue(AllowNavigationProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Fired when a day is tapped. Use for quick-schedule functionality.
    /// </summary>
    public event EventHandler<DateTime>? DayTapped;

    /// <summary>
    /// Fired when a day is long-pressed. Use for viewing day details.
    /// </summary>
    public event EventHandler<DateTime>? DayLongPressed;

    /// <summary>
    /// Fired when the displayed month changes.
    /// </summary>
    public event EventHandler<DateTime>? MonthChanged;

    #endregion

    public MonthCalendarControl()
    {
        InitializeComponent();
        BuildCalendar();
    }

    #region Property Changed Handlers

    private static void OnSelectedDateChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MonthCalendarControl control)
        {
            var newDate = (DateTime)newValue;
            if (newDate.Year != control.DisplayMonth.Year || newDate.Month != control.DisplayMonth.Month)
            {
                control.DisplayMonth = new DateTime(newDate.Year, newDate.Month, 1);
            }
            else
            {
                control.BuildCalendar();
            }
        }
    }

    private static void OnDisplayMonthChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MonthCalendarControl control)
        {
            control.BuildCalendar();
            control.MonthChanged?.Invoke(control, (DateTime)newValue);
        }
    }

    private static void OnJobCountsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MonthCalendarControl control)
        {
            control.BuildCalendar();
        }
    }

    private static void OnAllowNavigationChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MonthCalendarControl control)
        {
            var allow = (bool)newValue;
            control.PrevButton.IsVisible = allow;
            control.NextButton.IsVisible = allow;
            control.TodayButton.IsVisible = allow;
        }
    }

    #endregion

    #region Calendar Building

    private void BuildCalendar()
    {
        CalendarGrid.Children.Clear();
        CalendarGrid.RowDefinitions.Clear();

        var firstOfMonth = new DateTime(DisplayMonth.Year, DisplayMonth.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(DisplayMonth.Year, DisplayMonth.Month);
        var startDayOfWeek = (int)firstOfMonth.DayOfWeek;

        // Calculate rows needed
        var totalCells = startDayOfWeek + daysInMonth;
        var rows = (int)Math.Ceiling(totalCells / 7.0);

        for (int i = 0; i < rows; i++)
        {
            CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) });
        }

        // Update header
        MonthYearLabel.Text = DisplayMonth.ToString("MMMM yyyy");

        // Fill calendar
        var today = DateTime.Today;
        var currentDay = 1;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                var cellIndex = row * 7 + col;
                var isValidDay = cellIndex >= startDayOfWeek && currentDay <= daysInMonth;

                if (isValidDay)
                {
                    var date = new DateTime(DisplayMonth.Year, DisplayMonth.Month, currentDay);
                    var dayCell = CreateDayCell(date, today);
                    Grid.SetRow(dayCell, row);
                    Grid.SetColumn(dayCell, col);
                    CalendarGrid.Children.Add(dayCell);
                    currentDay++;
                }
                else
                {
                    // Empty cell for padding
                    var emptyCell = new BoxView { Color = Colors.Transparent };
                    Grid.SetRow(emptyCell, row);
                    Grid.SetColumn(emptyCell, col);
                    CalendarGrid.Children.Add(emptyCell);
                }
            }
        }
    }

    private View CreateDayCell(DateTime date, DateTime today)
    {
        var isToday = date == today;
        var isSelected = date == SelectedDate.Date;
        var isPast = date < today;
        var isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

        // Get job count for this day
        var jobCount = JobCounts?.TryGetValue(date, out var count) == true ? count : 0;

        var cell = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Stroke = isSelected ? AppColors.Primary : Colors.Transparent,
            StrokeThickness = isSelected ? 2 : 0,
            Padding = 4
        };

        // Background color
        if (isToday)
        {
            cell.BackgroundColor = AppColors.Primary;
        }
        else if (isSelected)
        {
            cell.BackgroundColor = AppColors.PrimarySurface;
        }
        else if (isWeekend)
        {
            cell.SetAppThemeColor(Border.BackgroundColorProperty, Color.FromArgb("#FAFAFA"), Color.FromArgb("#3D3D3D"));
        }
        else
        {
            cell.BackgroundColor = Colors.Transparent;
        }

        var content = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 2
        };

        // Day number
        var dayLabel = new Label
        {
            Text = date.Day.ToString(),
            FontSize = 14,
            FontAttributes = isToday ? FontAttributes.Bold : FontAttributes.None,
            HorizontalTextAlignment = TextAlignment.Center
        };

        if (isToday)
        {
            dayLabel.TextColor = Colors.White;
        }
        else if (isPast)
        {
            dayLabel.SetAppThemeColor(Label.TextColorProperty, AppColors.TextTertiary, Color.FromArgb("#666666"));
        }
        else
        {
            dayLabel.SetAppThemeColor(Label.TextColorProperty, AppColors.TextPrimary, Colors.White);
        }

        content.Children.Add(dayLabel);

        // Job indicator dots
        if (jobCount > 0)
        {
            var dotsLayout = new HorizontalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 2
            };

            var dotCount = Math.Min(jobCount, 3); // Show max 3 dots
            for (int i = 0; i < dotCount; i++)
            {
                var dot = new BoxView
                {
                    WidthRequest = 4,
                    HeightRequest = 4,
                    CornerRadius = 2,
                    Color = isToday ? Colors.White : AppColors.Primary
                };
                dotsLayout.Children.Add(dot);
            }

            if (jobCount > 3)
            {
                var moreLabel = new Label
                {
                    Text = "+",
                    FontSize = 8,
                    TextColor = isToday ? Colors.White : AppColors.Primary,
                    VerticalOptions = LayoutOptions.Center
                };
                dotsLayout.Children.Add(moreLabel);
            }

            content.Children.Add(dotsLayout);
        }

        cell.Content = content;

        // Add tap gesture
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) => OnDayCellTapped(date);
        cell.GestureRecognizers.Add(tapGesture);

        // Add long-press gesture (for viewing day details)
        var longPressGesture = new PointerGestureRecognizer();
        // Note: For true long-press, you might need a custom handler
        // For now, we'll use the tap for selection and the event for scheduling

        return cell;
    }

    private void OnDayCellTapped(DateTime date)
    {
        SelectedDate = date;
        DayTapped?.Invoke(this, date);

        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }

    #endregion

    #region Event Handlers

    private void OnPreviousMonthClicked(object? sender, EventArgs e)
    {
        DisplayMonth = DisplayMonth.AddMonths(-1);
    }

    private void OnNextMonthClicked(object? sender, EventArgs e)
    {
        DisplayMonth = DisplayMonth.AddMonths(1);
    }

    private void OnTodayClicked(object? sender, EventArgs e)
    {
        DisplayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        SelectedDate = DateTime.Today;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Refreshes the calendar display with current job counts.
    /// </summary>
    public void Refresh()
    {
        BuildCalendar();
    }

    /// <summary>
    /// Navigates to a specific date.
    /// </summary>
    public void GoToDate(DateTime date)
    {
        DisplayMonth = new DateTime(date.Year, date.Month, 1);
        SelectedDate = date;
    }

    #endregion
}
