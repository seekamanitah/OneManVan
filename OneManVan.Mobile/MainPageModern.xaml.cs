using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;

namespace OneManVan.Mobile;

public partial class MainPageModern : ContentPage
{
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;
    public bool IsRefreshing { get; set; }

    public MainPageModern(IDbContextFactory<OneManVanDbContext> dbFactory)
    {
        InitializeComponent();
        _dbFactory = dbFactory;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDashboardDataAsync();
    }

    private async Task LoadDashboardDataAsync()
    {
        try
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            await using var db = await _dbFactory.CreateDbContextAsync();

            // Load metrics
            var totalRevenue = await db.Invoices
                .Where(i => i.Status == Shared.Models.Enums.InvoiceStatus.Paid)
                .SumAsync(i => i.Total);

            var activeJobsCount = await db.Jobs
                .CountAsync(j => j.Status == Shared.Models.Enums.JobStatus.InProgress ||
                                 j.Status == Shared.Models.Enums.JobStatus.Scheduled);

            var customersCount = await db.Customers.CountAsync();
            var invoicesCount = await db.Invoices.CountAsync();

            // Update UI on main thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                RevenueLabel.Text = $"${totalRevenue:N0}";
                JobsLabel.Text = activeJobsCount.ToString();
                CustomersLabel.Text = customersCount.ToString();
                InvoicesLabel.Text = invoicesCount.ToString();
            });

            // Load recent activity
            await LoadRecentActivityAsync(db);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load dashboard: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            IsRefreshing = false;
            OnPropertyChanged(nameof(IsRefreshing));
        }
    }

    private async Task LoadRecentActivityAsync(OneManVanDbContext db)
    {
        var recentJobs = await db.Jobs
            .Include(j => j.Customer)
            .OrderByDescending(j => j.CreatedAt)
            .Take(10)
            .ToListAsync();

        var activityItems = recentJobs.Select(j => new ActivityItem
        {
            Title = j.Title ?? "Untitled Job",
            Subtitle = j.Customer?.DisplayName ?? "Unknown Customer",
            Status = j.Status.ToString(),
            StatusColor = GetStatusColor(j.Status),
            Date = j.ScheduledDate ?? j.CreatedAt,
            Amount = 0 // Jobs don't have amount, will show invoices later
        }).ToList();

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            RecentActivityList.ItemsSource = activityItems;
        });
    }

    private Color GetStatusColor(Shared.Models.Enums.JobStatus status)
    {
        return status switch
        {
            Shared.Models.Enums.JobStatus.Scheduled => Color.FromArgb("#10b981"), // Success
            Shared.Models.Enums.JobStatus.InProgress => Color.FromArgb("#f59e0b"), // Warning
            Shared.Models.Enums.JobStatus.Completed => Color.FromArgb("#06b6d4"),  // Info
            Shared.Models.Enums.JobStatus.Cancelled => Color.FromArgb("#6b7280"),  // Gray
            _ => Color.FromArgb("#3b82f6") // Primary
        };
    }

    private async void OnViewAllClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Jobs");
    }

    private async void OnNewCustomerClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Customers");
    }

    private async void OnNewJobClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AddJob");
    }

    // Activity Item Model
    private class ActivityItem
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Color StatusColor { get; set; } = Colors.Gray;
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }
}
