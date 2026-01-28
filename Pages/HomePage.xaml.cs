using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using OneManVan.Services;

namespace OneManVan.Pages;

/// <summary>
/// Home dashboard page with stats and quick actions.
/// </summary>
public partial class HomePage : UserControl
{
    public HomePage()
    {
        InitializeComponent();
        DataContext = new HomeViewModel();
    }

    private void AddCustomer_Click(object sender, RoutedEventArgs e)
    {
        // Set pending action and trigger navigation
        NavigationRequest.SetPendingAction("Add");
        NavigationRequest.Navigate("Customers", "Add");
    }

    private void AddAsset_Click(object sender, RoutedEventArgs e)
    {
        // Set pending action and trigger navigation
        NavigationRequest.SetPendingAction("Add");
        NavigationRequest.Navigate("Assets", "Add");
    }

    private void CreateEstimate_Click(object sender, RoutedEventArgs e)
    {
        // Set pending action and trigger navigation
        NavigationRequest.SetPendingAction("Add");
        NavigationRequest.Navigate("Estimates", "Add");
    }

    private async void ExportBackup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Backup",
            Filter = "JSON Backup|*.json|ZIP Backup|*.zip|All Files|*.*",
            FileName = $"OneManVan_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var result = await App.BackupService.CreateBackupAsync(dialog.FileName);
                if (result.Success)
                {
                    MessageBox.Show($"Backup saved to:\n{result.FilePath}\n\nRecords: {result.RecordCount}", 
                        "Backup Complete",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Backup failed: {result.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Backup failed: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

/// <summary>
/// ViewModel for the home dashboard.
/// </summary>
public class HomeViewModel : ViewModels.BaseViewModel
{
    private int _customerCount;
    private int _assetCount;
    private int _siteCount;
    private int _expiringWarrantyCount;
    private int _activeJobCount;
    private int _pendingInvoiceCount;
    private decimal _outstandingBalance;
    private string _hvacTip = string.Empty;
    private string _hvacTipTitle = string.Empty;

    public DateTime CurrentDate => DateTime.Now;

    public int CustomerCount
    {
        get => _customerCount;
        set => SetProperty(ref _customerCount, value);
    }

    public int AssetCount
    {
        get => _assetCount;
        set => SetProperty(ref _assetCount, value);
    }

    public int SiteCount
    {
        get => _siteCount;
        set => SetProperty(ref _siteCount, value);
    }

    public int ExpiringWarrantyCount
    {
        get => _expiringWarrantyCount;
        set => SetProperty(ref _expiringWarrantyCount, value);
    }

    public int ActiveJobCount
    {
        get => _activeJobCount;
        set => SetProperty(ref _activeJobCount, value);
    }

    public int PendingInvoiceCount
    {
        get => _pendingInvoiceCount;
        set => SetProperty(ref _pendingInvoiceCount, value);
    }

    public decimal OutstandingBalance
    {
        get => _outstandingBalance;
        set => SetProperty(ref _outstandingBalance, value);
    }

    public string HvacTip
    {
        get => _hvacTip;
        set => SetProperty(ref _hvacTip, value);
    }

    public string HvacTipTitle
    {
        get => _hvacTipTitle;
        set => SetProperty(ref _hvacTipTitle, value);
    }

    public bool HasNoAlerts => WarrantyAlerts.Count == 0;
    public bool HasNoActivity => RecentActivity.Count == 0;

    public ObservableCollection<WarrantyAlertItem> WarrantyAlerts { get; } = [];
    public ObservableCollection<RecentActivityItem> RecentActivity { get; } = [];

    public HomeViewModel()
    {
        LoadDashboardData();
        LoadRandomTip();
    }

    private async void LoadDashboardData()
    {
        try
        {
            CustomerCount = await App.DbContext.Customers.CountAsync();
            AssetCount = await App.DbContext.Assets.CountAsync();
            SiteCount = await App.DbContext.Sites.CountAsync();

            // Job stats
            ActiveJobCount = await App.DbContext.Jobs
                .CountAsync(j => j.Status == Shared.Models.Enums.JobStatus.InProgress || 
                                 j.Status == Shared.Models.Enums.JobStatus.Scheduled);

            // Invoice stats
            var unpaidInvoices = await App.DbContext.Invoices
                .Where(i => i.Status != Shared.Models.Enums.InvoiceStatus.Paid && 
                           i.Status != Shared.Models.Enums.InvoiceStatus.Cancelled &&
                           i.Status != Shared.Models.Enums.InvoiceStatus.Refunded)
                .AsNoTracking()
                .ToListAsync();

            PendingInvoiceCount = unpaidInvoices.Count;
            OutstandingBalance = unpaidInvoices.Sum(i => i.Total - i.AmountPaid);

            // Load recent activity
            await LoadRecentActivityAsync();

            // Get assets with warranties expiring in 90 days or already expired
            var assets = await App.DbContext.Assets
                .Include(a => a.Customer)
                .Where(a => a.WarrantyStartDate != null)
                .AsNoTracking()
                .ToListAsync();

            WarrantyAlerts.Clear();
            foreach (var asset in assets)
            {
                if (asset.WarrantyEndDate == null) continue;

                var daysLeft = asset.DaysUntilWarrantyExpires;
                if (daysLeft == null) continue;

                if (daysLeft <= 90)
                {
                    WarrantyAlerts.Add(new WarrantyAlertItem
                    {
                        Serial = asset.Serial,
                        CustomerName = asset.Customer.Name,
                        DaysLeft = daysLeft.Value,
                        IsExpired = daysLeft < 0,
                        StatusText = daysLeft < 0 
                            ? $"Expired {Math.Abs(daysLeft.Value)} days ago"
                            : $"Expires in {daysLeft} days"
                    });
                }
            }

            ExpiringWarrantyCount = WarrantyAlerts.Count;
            OnPropertyChanged(nameof(HasNoAlerts));
            OnPropertyChanged(nameof(HasNoActivity));
        }
        catch
        {
            // Silently fail on dashboard load errors
        }
    }

    private async Task LoadRecentActivityAsync()
    {
        RecentActivity.Clear();
        var activities = new List<RecentActivityItem>();

        // Recent customers
        var recentCustomers = await App.DbContext.Customers
            .OrderByDescending(c => c.CreatedAt)
            .Take(3)
            .AsNoTracking()
            .ToListAsync();

        foreach (var customer in recentCustomers)
        {
            activities.Add(new RecentActivityItem
            {
                Icon = "",
                Title = $"Customer added: {customer.Name}",
                Description = customer.Email ?? customer.Phone ?? "No contact info",
                Timestamp = customer.CreatedAt,
                TimeAgo = GetTimeAgo(customer.CreatedAt)
            });
        }

        // Recent jobs
        var recentJobs = await App.DbContext.Jobs
            .Include(j => j.Customer)
            .OrderByDescending(j => j.CreatedAt)
            .Take(3)
            .AsNoTracking()
            .ToListAsync();

        foreach (var job in recentJobs)
        {
            activities.Add(new RecentActivityItem
            {
                Icon = "",
                Title = $"Job: {job.Title}",
                Description = $"{job.Customer.Name} - {job.StatusDisplay}",
                Timestamp = job.CreatedAt,
                TimeAgo = GetTimeAgo(job.CreatedAt)
            });
        }

        // Recent invoices
        var recentInvoices = await App.DbContext.Invoices
            .Include(i => i.Customer)
            .OrderByDescending(i => i.CreatedAt)
            .Take(3)
            .AsNoTracking()
            .ToListAsync();

        foreach (var invoice in recentInvoices)
        {
            activities.Add(new RecentActivityItem
            {
                Icon = "",
                Title = $"Invoice: {invoice.InvoiceNumber}",
                Description = $"{invoice.Customer.Name} - ${invoice.Total:N2}",
                Timestamp = invoice.CreatedAt,
                TimeAgo = GetTimeAgo(invoice.CreatedAt)
            });
        }

        // Recent assets
        var recentAssets = await App.DbContext.Assets
            .Include(a => a.Customer)
            .OrderByDescending(a => a.CreatedAt)
            .Take(3)
            .AsNoTracking()
            .ToListAsync();

        foreach (var asset in recentAssets)
        {
            activities.Add(new RecentActivityItem
            {
                Icon = "",
                Title = $"Asset: {asset.Brand} {asset.Model}".Trim(),
                Description = $"{asset.Customer.Name} - S/N: {asset.Serial}",
                Timestamp = asset.CreatedAt,
                TimeAgo = GetTimeAgo(asset.CreatedAt)
            });
        }

        // Sort by timestamp and take top 5
        foreach (var activity in activities.OrderByDescending(a => a.Timestamp).Take(5))
        {
            RecentActivity.Add(activity);
        }
    }

    private static string GetTimeAgo(DateTime timestamp)
    {
        var diff = DateTime.UtcNow - timestamp;

        if (diff.TotalMinutes < 1)
            return "Just now";
        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays}d ago";
        if (diff.TotalDays < 30)
            return $"{(int)(diff.TotalDays / 7)}w ago";
        
        return timestamp.ToString("MMM d");
    }

    private void LoadRandomTip()
    {
        var tip = QuickStartGuideService.GetRandomTip();
        HvacTipTitle = tip.Title;
        HvacTip = tip.Content;
    }
}

/// <summary>
/// Model for warranty alert display.
/// </summary>
public class WarrantyAlertItem
{
    public string Serial { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int DaysLeft { get; set; }
    public bool IsExpired { get; set; }
    public string StatusText { get; set; } = string.Empty;
}

/// <summary>
/// Model for recent activity display.
/// </summary>
public class RecentActivityItem
{
    public string Icon { get; set; } = "";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string TimeAgo { get; set; } = string.Empty;
}
