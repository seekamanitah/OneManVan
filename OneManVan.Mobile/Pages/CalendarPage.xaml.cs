using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

public partial class CalendarPage : ContentPage
{
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;
    private DateTime _currentMonth;
    private DateTime? _selectedDate;
    private List<Job> _monthJobs = new();
    private Dictionary<DateTime, List<Job>> _jobsByDate = new();

    public CalendarPage(IDbContextFactory<OneManVanDbContext> dbFactory)
    {
        InitializeComponent();
        _dbFactory = dbFactory;
        _currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadMonthDataAsync();
        BuildCalendarGrid();
    }

    private async Task LoadMonthDataAsync()
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var startOfMonth = _currentMonth;
            var endOfMonth = _currentMonth.AddMonths(1).AddDays(-1);

            _monthJobs = await db.Jobs
                .Include(j => j.Customer)
                .Where(j => j.ScheduledDate >= startOfMonth && j.ScheduledDate <= endOfMonth)
                .OrderBy(j => j.ScheduledDate)
                .ThenBy(j => j.ArrivalWindowStart)
                .ToListAsync();

            _jobsByDate = _monthJobs
                .Where(j => j.ScheduledDate.HasValue)
                .GroupBy(j => j.ScheduledDate!.Value.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            MonthYearLabel.Text = _currentMonth.ToString("MMMM yyyy");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading calendar data: {ex.Message}");
        }
    }

    private void BuildCalendarGrid()
    {
        CalendarGrid.Children.Clear();
        CalendarGrid.RowDefinitions.Clear();
        CalendarGrid.ColumnDefinitions.Clear();

        // 7 columns for days of week
        for (int i = 0; i < 7; i++)
            CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        // Calculate grid dimensions
        var firstDayOfMonth = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
        var startDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
        var totalCells = startDayOfWeek + daysInMonth;
        var rows = (int)Math.Ceiling(totalCells / 7.0);

        for (int i = 0; i < rows; i++)
            CalendarGrid.RowDefinitions.Add(new RowDefinition(new GridLength(60)));

        // Add day cells
        for (int day = 1; day <= daysInMonth; day++)
        {
            var currentDate = new DateTime(_currentMonth.Year, _currentMonth.Month, day);
            var cellIndex = startDayOfWeek + day - 1;
            var row = cellIndex / 7;
            var col = cellIndex % 7;

            var dayCell = CreateDayCell(currentDate);
            CalendarGrid.Add(dayCell, col, row);
        }
    }

    private View CreateDayCell(DateTime date)
    {
        var hasJobs = _jobsByDate.ContainsKey(date.Date);
        var jobCount = hasJobs ? _jobsByDate[date.Date].Count : 0;
        var isToday = date.Date == DateTime.Today;
        var isSelected = _selectedDate.HasValue && date.Date == _selectedDate.Value.Date;

        var frame = new Frame
        {
            Padding = new Thickness(4),
            Margin = new Thickness(1),
            CornerRadius = 4,
            HasShadow = false,
            BackgroundColor = isSelected ? Color.FromArgb("#E3F2FD") : 
                              isToday ? Color.FromArgb("#FFF3E0") : Colors.White,
            BorderColor = isSelected ? Color.FromArgb("#1976D2") : Colors.Transparent
        };

        var stack = new VerticalStackLayout
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Spacing = 2
        };

        var dayLabel = new Label
        {
            Text = date.Day.ToString(),
            FontSize = 14,
            FontAttributes = isToday ? FontAttributes.Bold : FontAttributes.None,
            HorizontalOptions = LayoutOptions.Center,
            TextColor = isToday ? Color.FromArgb("#FF5722") : Colors.Black
        };
        stack.Children.Add(dayLabel);

        if (hasJobs)
        {
            var indicator = new Frame
            {
                WidthRequest = 20,
                HeightRequest = 14,
                CornerRadius = 7,
                Padding = 0,
                BackgroundColor = GetStatusColor(_jobsByDate[date.Date].First().Status),
                HorizontalOptions = LayoutOptions.Center
            };

            var countLabel = new Label
            {
                Text = jobCount.ToString(),
                FontSize = 9,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            indicator.Content = countLabel;
            stack.Children.Add(indicator);
        }

        frame.Content = stack;

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) => OnDayTapped(date);
        frame.GestureRecognizers.Add(tapGesture);

        return frame;
    }

    private Color GetStatusColor(JobStatus status) => status switch
    {
        JobStatus.Draft => Color.FromArgb("#007bff"),
        JobStatus.Scheduled => Color.FromArgb("#28a745"),
        JobStatus.InProgress => Color.FromArgb("#ffc107"),
        JobStatus.Completed => Color.FromArgb("#17a2b8"),
        JobStatus.Cancelled => Color.FromArgb("#6c757d"),
        _ => Color.FromArgb("#6c757d")
    };

    private void OnDayTapped(DateTime date)
    {
        _selectedDate = date;
        BuildCalendarGrid();

        SelectedDayLabel.Text = date.ToString("dddd, MMMM d, yyyy");
        SelectedDayPanel.IsVisible = true;

        if (_jobsByDate.TryGetValue(date.Date, out var jobs))
        {
            JobsCollectionView.ItemsSource = jobs.Select(j => new
            {
                j.Id,
                j.Title,
                j.Status,
                CustomerName = j.Customer?.DisplayName ?? "No Customer",
                ScheduledTime = j.ArrivalWindowStart ?? TimeSpan.Zero
            }).ToList();
        }
        else
        {
            JobsCollectionView.ItemsSource = null;
        }
    }

    private async void OnJobSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not null)
        {
            var selectedItem = e.CurrentSelection.First();
            var idProperty = selectedItem.GetType().GetProperty("Id");
            if (idProperty != null)
            {
                var id = (int)idProperty.GetValue(selectedItem)!;
                await Shell.Current.GoToAsync($"JobDetail?id={id}");
            }
            ((CollectionView)sender).SelectedItem = null;
        }
    }

    private async void OnPreviousMonthClicked(object sender, EventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(-1);
        _selectedDate = null;
        SelectedDayPanel.IsVisible = false;
        await LoadMonthDataAsync();
        BuildCalendarGrid();
    }

    private async void OnNextMonthClicked(object sender, EventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(1);
        _selectedDate = null;
        SelectedDayPanel.IsVisible = false;
        await LoadMonthDataAsync();
        BuildCalendarGrid();
    }
}
