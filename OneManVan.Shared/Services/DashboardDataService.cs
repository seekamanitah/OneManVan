using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Services;

/// <summary>
/// Service for calculating dashboard metrics and business intelligence.
/// </summary>
public class DashboardDataService
{
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;

    public DashboardDataService(IDbContextFactory<OneManVanDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Get comprehensive dashboard metrics.
    /// </summary>
    public async Task<DashboardMetrics> GetMetricsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        
        var now = DateTime.Now;
        var today = now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek); // Sunday
        var weekEnd = weekStart.AddDays(7);
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var lastWeekStart = weekStart.AddDays(-7);
        var lastWeekEnd = weekStart;

        var metrics = new DashboardMetrics();

        // ===== TODAY =====
        
        // Today's revenue (paid invoices)
        metrics.TodayRevenue = await db.Invoices
            .Where(i => i.PaidAt.HasValue && i.PaidAt.Value.Date == today)
            .SumAsync(i => i.Total);

        // Today's job count
        metrics.TodayJobCount = await db.Jobs
            .Where(j => j.ScheduledDate.HasValue && j.ScheduledDate.Value.Date == today)
            .CountAsync();

        // Today's completed jobs
        metrics.TodayCompletedJobs = await db.Jobs
            .Where(j => j.ScheduledDate.HasValue && 
                       j.ScheduledDate.Value.Date == today &&
                       j.Status == JobStatus.Completed)
            .CountAsync();

        // Active jobs (in progress)
        metrics.ActiveJobs = await db.Jobs
            .Where(j => j.Status == JobStatus.InProgress)
            .CountAsync();

        // Overdue jobs (past scheduled date, not completed)
        metrics.OverdueJobs = await db.Jobs
            .Where(j => j.ScheduledDate.HasValue &&
                       j.ScheduledDate.Value < today &&
                       j.Status != JobStatus.Completed &&
                       j.Status != JobStatus.Cancelled)
            .CountAsync();

        // ===== THIS WEEK =====
        
        // Week revenue
        metrics.WeekRevenue = await db.Invoices
            .Where(i => i.PaidAt.HasValue && 
                       i.PaidAt.Value >= weekStart && 
                       i.PaidAt.Value < weekEnd)
            .SumAsync(i => i.Total);

        // Week job count
        metrics.WeekJobCount = await db.Jobs
            .Where(j => j.ScheduledDate.HasValue &&
                       j.ScheduledDate.Value >= weekStart &&
                       j.ScheduledDate.Value < weekEnd)
            .CountAsync();

        // Week completed jobs
        metrics.WeekCompletedJobs = await db.Jobs
            .Where(j => j.ScheduledDate.HasValue &&
                       j.ScheduledDate.Value >= weekStart &&
                       j.ScheduledDate.Value < weekEnd &&
                       j.Status == JobStatus.Completed)
            .CountAsync();

        // New customers this week
        metrics.NewCustomersWeek = await db.Customers
            .Where(c => c.CreatedAt >= weekStart && c.CreatedAt < weekEnd)
            .CountAsync();

        // ===== THIS MONTH =====
        
        // Month revenue
        metrics.MonthRevenue = await db.Invoices
            .Where(i => i.PaidAt.HasValue && i.PaidAt.Value >= monthStart)
            .SumAsync(i => i.Total);

        // Month job count
        metrics.MonthJobCount = await db.Jobs
            .Where(j => j.ScheduledDate.HasValue && j.ScheduledDate.Value >= monthStart)
            .CountAsync();

        // Average job value
        if (metrics.MonthJobCount > 0)
        {
            metrics.AvgJobValue = metrics.MonthRevenue / metrics.MonthJobCount;
        }

        // Customer growth this month
        metrics.CustomerGrowth = await db.Customers
            .Where(c => c.CreatedAt >= monthStart)
            .CountAsync();

        // ===== TRENDS =====
        
        // Revenue trend (last 7 days)
        for (int i = 6; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            var dayRevenue = await db.Invoices
                .Where(inv => inv.PaidAt.HasValue && inv.PaidAt.Value.Date == day)
                .SumAsync(inv => inv.Total);
            metrics.RevenueTrend.Add(dayRevenue);
        }

        // Job completion rate (this week)
        if (metrics.WeekJobCount > 0)
        {
            metrics.CompletionRate = (double)metrics.WeekCompletedJobs / metrics.WeekJobCount * 100;
        }

        // Revenue change vs last week
        var lastWeekRevenue = await db.Invoices
            .Where(i => i.PaidAt.HasValue &&
                       i.PaidAt.Value >= lastWeekStart &&
                       i.PaidAt.Value < lastWeekEnd)
            .SumAsync(i => i.Total);

        if (lastWeekRevenue > 0)
        {
            metrics.RevenueChangePercent = 
                (double)((metrics.WeekRevenue - lastWeekRevenue) / lastWeekRevenue * 100);
        }

        // Jobs change vs last week
        var lastWeekJobCount = await db.Jobs
            .Where(j => j.ScheduledDate.HasValue &&
                       j.ScheduledDate.Value >= lastWeekStart &&
                       j.ScheduledDate.Value < lastWeekEnd)
            .CountAsync();

        if (lastWeekJobCount > 0)
        {
            metrics.JobsChangePercent = 
                (double)((metrics.WeekJobCount - lastWeekJobCount) / (double)lastWeekJobCount * 100);
        }

        return metrics;
    }

    /// <summary>
    /// Get next scheduled job.
    /// </summary>
    public async Task<Job?> GetNextJobAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        
        return await db.Jobs
            .Include(j => j.Customer)
            .Include(j => j.Site)
            .Where(j => j.ScheduledDate.HasValue &&
                       j.ScheduledDate.Value >= DateTime.Now &&
                       j.Status == JobStatus.Scheduled)
            .OrderBy(j => j.ScheduledDate)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Get today's jobs.
    /// </summary>
    public async Task<List<Job>> GetTodayJobsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        
        var today = DateTime.Now.Date;
        
        return await db.Jobs
            .Include(j => j.Customer)
            .Include(j => j.Site)
            .Where(j => j.ScheduledDate.HasValue && j.ScheduledDate.Value.Date == today)
            .OrderBy(j => j.ScheduledDate)
            .ToListAsync();
    }

    /// <summary>
    /// Get dashboard alerts and notifications.
    /// </summary>
    public async Task<List<DashboardAlert>> GetAlertsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var alerts = new List<DashboardAlert>();

        // Overdue invoices
        var overdueInvoices = await db.Invoices
            .Where(i => i.DueDate < DateTime.Now && 
                       i.Status != InvoiceStatus.Paid && 
                       i.Status != InvoiceStatus.Cancelled)
            .CountAsync();

        if (overdueInvoices > 0)
        {
            alerts.Add(new DashboardAlert
            {
                Title = "Overdue Invoices",
                Description = $"{overdueInvoices} invoice{(overdueInvoices > 1 ? "s" : "")} past due date",
                Type = AlertType.Danger,
                Count = overdueInvoices,
                ActionUrl = "/invoices"
            });
        }

        // Unregistered assets (assets with warranty but not registered)
        var unregisteredAssets = await db.Assets
            .Where(a => a.WarrantyExpiration.HasValue && 
                       a.WarrantyExpiration.Value > DateTime.Now &&
                       !a.IsRegisteredOnline)
            .CountAsync();

        if (unregisteredAssets > 0)
        {
            alerts.Add(new DashboardAlert
            {
                Title = "Unregistered Assets",
                Description = $"{unregisteredAssets} asset{(unregisteredAssets > 1 ? "s" : "")} need warranty registration",
                Type = AlertType.Warning,
                Count = unregisteredAssets,
                ActionUrl = "/assets"
            });
        }

        // Expiring warranties (within 30 days)
        var expiringWarranties = await db.Assets
            .Where(a => a.WarrantyExpiration.HasValue &&
                       a.WarrantyExpiration.Value > DateTime.Now &&
                       a.WarrantyExpiration.Value <= DateTime.Now.AddDays(30))
            .CountAsync();

        if (expiringWarranties > 0)
        {
            alerts.Add(new DashboardAlert
            {
                Title = "Expiring Warranties",
                Description = $"{expiringWarranties} warrant{(expiringWarranties > 1 ? "ies" : "y")} expiring soon",
                Type = AlertType.Warning,
                Count = expiringWarranties,
                ActionUrl = "/assets"
            });
        }

        // Low inventory items (quantity < 5)
        var lowInventory = await db.InventoryItems
            .Where(i => i.QuantityOnHand < 5 && i.QuantityOnHand > 0)
            .CountAsync();

        if (lowInventory > 0)
        {
            alerts.Add(new DashboardAlert
            {
                Title = "Low Inventory",
                Description = $"{lowInventory} item{(lowInventory > 1 ? "s" : "")} running low",
                Type = AlertType.Info,
                Count = lowInventory,
                ActionUrl = "/inventory"
            });
        }

        // Pending estimates (not converted to jobs)
        var pendingEstimates = await db.Estimates
            .Where(e => e.Status == EstimateStatus.Sent && 
                       e.CreatedAt > DateTime.Now.AddDays(-30))
            .CountAsync();

        if (pendingEstimates > 0)
        {
            alerts.Add(new DashboardAlert
            {
                Title = "Pending Estimates",
                Description = $"{pendingEstimates} estimate{(pendingEstimates > 1 ? "s" : "")} awaiting response",
                Type = AlertType.Info,
                Count = pendingEstimates,
                ActionUrl = "/estimates"
            });
        }

        return alerts.OrderByDescending(a => a.Type).ThenByDescending(a => a.Count).ToList();
    }
}
