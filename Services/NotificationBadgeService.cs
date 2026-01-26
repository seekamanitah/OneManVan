using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Services;

/// <summary>
/// Service for managing notification badges across the application.
/// </summary>
public class NotificationBadgeService : INotifyPropertyChanged
{
    private static NotificationBadgeService? _instance;
    private int _warrantyAlertCount;
    private int _overdueInvoiceCount;
    private int _lowStockCount;
    private int _pendingJobCount;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static NotificationBadgeService Instance => _instance ??= new NotificationBadgeService();

    /// <summary>
    /// Number of warranty alerts (expiring in 90 days or expired).
    /// </summary>
    public int WarrantyAlertCount
    {
        get => _warrantyAlertCount;
        private set
        {
            if (_warrantyAlertCount != value)
            {
                _warrantyAlertCount = value;
                OnPropertyChanged(nameof(WarrantyAlertCount));
                OnPropertyChanged(nameof(HasWarrantyAlerts));
                OnPropertyChanged(nameof(TotalAlertCount));
            }
        }
    }

    /// <summary>
    /// Number of overdue invoices.
    /// </summary>
    public int OverdueInvoiceCount
    {
        get => _overdueInvoiceCount;
        private set
        {
            if (_overdueInvoiceCount != value)
            {
                _overdueInvoiceCount = value;
                OnPropertyChanged(nameof(OverdueInvoiceCount));
                OnPropertyChanged(nameof(HasOverdueInvoices));
                OnPropertyChanged(nameof(TotalAlertCount));
            }
        }
    }

    /// <summary>
    /// Number of low stock inventory items.
    /// </summary>
    public int LowStockCount
    {
        get => _lowStockCount;
        private set
        {
            if (_lowStockCount != value)
            {
                _lowStockCount = value;
                OnPropertyChanged(nameof(LowStockCount));
                OnPropertyChanged(nameof(HasLowStock));
                OnPropertyChanged(nameof(TotalAlertCount));
            }
        }
    }

    /// <summary>
    /// Number of pending jobs (scheduled or in progress).
    /// </summary>
    public int PendingJobCount
    {
        get => _pendingJobCount;
        private set
        {
            if (_pendingJobCount != value)
            {
                _pendingJobCount = value;
                OnPropertyChanged(nameof(PendingJobCount));
                OnPropertyChanged(nameof(HasPendingJobs));
            }
        }
    }

    /// <summary>
    /// Total count of all alerts.
    /// </summary>
    public int TotalAlertCount => WarrantyAlertCount + OverdueInvoiceCount + LowStockCount;

    /// <summary>
    /// Whether there are any warranty alerts.
    /// </summary>
    public bool HasWarrantyAlerts => WarrantyAlertCount > 0;

    /// <summary>
    /// Whether there are any overdue invoices.
    /// </summary>
    public bool HasOverdueInvoices => OverdueInvoiceCount > 0;

    /// <summary>
    /// Whether there are any low stock items.
    /// </summary>
    public bool HasLowStock => LowStockCount > 0;

    /// <summary>
    /// Whether there are any pending jobs.
    /// </summary>
    public bool HasPendingJobs => PendingJobCount > 0;

    /// <summary>
    /// Whether there are any alerts at all.
    /// </summary>
    public bool HasAnyAlerts => TotalAlertCount > 0;

    /// <summary>
    /// Refreshes all badge counts from the database.
    /// </summary>
    public async Task RefreshAllAsync()
    {
        await RefreshWarrantyAlertsAsync();
        await RefreshOverdueInvoicesAsync();
        await RefreshLowStockAsync();
        await RefreshPendingJobsAsync();
    }

    /// <summary>
    /// Refreshes warranty alert count.
    /// </summary>
    public async Task RefreshWarrantyAlertsAsync()
    {
        try
        {
            var assets = await App.DbContext.Assets
                .Where(a => a.WarrantyStartDate != null)
                .AsNoTracking()
                .ToListAsync();

            var count = 0;
            foreach (var asset in assets)
            {
                var daysLeft = asset.DaysUntilWarrantyExpires;
                if (daysLeft.HasValue && daysLeft <= 90)
                    count++;
            }

            WarrantyAlertCount = count;
        }
        catch
        {
            // Silently ignore errors
        }
    }

    /// <summary>
    /// Refreshes overdue invoice count.
    /// </summary>
    public async Task RefreshOverdueInvoicesAsync()
    {
        try
        {
            OverdueInvoiceCount = await App.DbContext.Invoices
                .CountAsync(i => i.Status == InvoiceStatus.Overdue ||
                                (i.Status == InvoiceStatus.Sent && i.DueDate < DateTime.UtcNow));
        }
        catch
        {
            // Silently ignore errors
        }
    }

    /// <summary>
    /// Refreshes low stock count.
    /// </summary>
    public async Task RefreshLowStockAsync()
    {
        try
        {
            LowStockCount = await App.DbContext.InventoryItems
                .CountAsync(i => i.IsActive && i.QuantityOnHand <= i.ReorderPoint);
        }
        catch
        {
            // Silently ignore errors
        }
    }

    /// <summary>
    /// Refreshes pending job count.
    /// </summary>
    public async Task RefreshPendingJobsAsync()
    {
        try
        {
            PendingJobCount = await App.DbContext.Jobs
                .CountAsync(j => j.Status == JobStatus.Scheduled || 
                                j.Status == JobStatus.InProgress ||
                                j.Status == JobStatus.EnRoute);
        }
        catch
        {
            // Silently ignore errors
        }
    }

    /// <summary>
    /// Gets the badge display text for a count.
    /// </summary>
    public static string GetBadgeText(int count)
    {
        if (count == 0) return string.Empty;
        if (count > 99) return "99+";
        return count.ToString();
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
