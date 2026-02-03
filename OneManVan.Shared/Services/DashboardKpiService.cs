using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Services;

/// <summary>
/// Service for calculating real-time dashboard KPIs and business metrics.
/// </summary>
public class DashboardKpiService
{
    private readonly IDbContextFactory<OneManVanDbContext> _contextFactory;

    public DashboardKpiService(IDbContextFactory<OneManVanDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Gets all dashboard KPIs in a single optimized call.
    /// </summary>
    public async Task<DashboardKpis> GetAllKpisAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var lastMonthStart = monthStart.AddMonths(-1);
        var lastMonthEnd = monthStart.AddDays(-1);
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        
        var kpis = new DashboardKpis();
        
        // === Revenue Metrics ===
        kpis.Revenue = await CalculateRevenueMetricsAsync(context, today, yesterday, monthStart, lastMonthStart, lastMonthEnd);
        
        // === Job Metrics ===
        kpis.Jobs = await CalculateJobMetricsAsync(context, today, weekStart);
        
        // === Customer Metrics ===
        kpis.Customers = await CalculateCustomerMetricsAsync(context, today);
        
        // === Receivables Aging ===
        kpis.Receivables = await CalculateReceivablesAsync(context, today);
        
        // === Estimates Pipeline ===
        kpis.Estimates = await CalculateEstimateMetricsAsync(context);
        
        // === Service Agreements ===
        kpis.ServiceAgreements = await CalculateServiceAgreementMetricsAsync(context, today);
        
        // === Inventory Alerts ===
            kpis.Inventory = await CalculateInventoryMetricsAsync(context);
        
            // === Employee Timesheets ===
            kpis.Employees = await CalculateEmployeeMetricsAsync(context, today, weekStart);
        
            kpis.GeneratedAt = DateTime.UtcNow;
        
            return kpis;
        }

    private async Task<RevenueMetrics> CalculateRevenueMetricsAsync(
        OneManVanDbContext context, DateTime today, DateTime yesterday, 
        DateTime monthStart, DateTime lastMonthStart, DateTime lastMonthEnd)
    {
        var metrics = new RevenueMetrics();
        
        // Today's revenue (paid invoices)
        metrics.TodayRevenue = await context.Invoices
            .Where(i => i.InvoiceDate >= today && i.InvoiceDate < today.AddDays(1))
            .Where(i => i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft)
            .SumAsync(i => (decimal?)i.Total) ?? 0;
        
        // Yesterday's revenue
        metrics.YesterdayRevenue = await context.Invoices
            .Where(i => i.InvoiceDate >= yesterday && i.InvoiceDate < today)
            .Where(i => i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft)
            .SumAsync(i => (decimal?)i.Total) ?? 0;
        
        // This month's revenue
        metrics.MonthRevenue = await context.Invoices
            .Where(i => i.InvoiceDate >= monthStart && i.InvoiceDate < today.AddDays(1))
            .Where(i => i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft)
            .SumAsync(i => (decimal?)i.Total) ?? 0;
        
        // Last month's revenue
        metrics.LastMonthRevenue = await context.Invoices
            .Where(i => i.InvoiceDate >= lastMonthStart && i.InvoiceDate <= lastMonthEnd)
            .Where(i => i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft)
            .SumAsync(i => (decimal?)i.Total) ?? 0;
        
        // Calculate change percentages
        if (metrics.YesterdayRevenue > 0)
            metrics.DayOverDayChange = ((metrics.TodayRevenue - metrics.YesterdayRevenue) / metrics.YesterdayRevenue) * 100;
        
        
        if (metrics.LastMonthRevenue > 0)
            metrics.MonthOverMonthChange = ((metrics.MonthRevenue - metrics.LastMonthRevenue) / metrics.LastMonthRevenue) * 100;
        
        // Total paid this month
        metrics.PaidThisMonth = await context.Payments
            .Where(p => p.PaymentDate >= monthStart)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;
        
        // Average days to payment
        var paidInvoices = await context.Invoices
            .Where(i => i.Status == InvoiceStatus.Paid && i.PaidAt.HasValue)
            .Where(i => i.InvoiceDate >= today.AddDays(-90))
            .Select(i => new { i.InvoiceDate, i.PaidAt })
            .ToListAsync();
        
        if (paidInvoices.Any())
        {
            metrics.AverageDaysToPayment = (decimal)paidInvoices
                .Average(i => (i.PaidAt!.Value - i.InvoiceDate).TotalDays);
        }
        
        return metrics;
    }

    private async Task<JobMetrics> CalculateJobMetricsAsync(
        OneManVanDbContext context, DateTime today, DateTime weekStart)
    {
        var metrics = new JobMetrics();
        
        // Today's jobs
        metrics.TodayJobCount = await context.Jobs
            .Where(j => j.ScheduledDate.HasValue)
            .Where(j => j.ScheduledDate!.Value.Date == today)
            .CountAsync();
        
        // This week's jobs
        metrics.WeekJobCount = await context.Jobs
            .Where(j => j.ScheduledDate.HasValue)
            .Where(j => j.ScheduledDate >= weekStart && j.ScheduledDate < weekStart.AddDays(7))
            .CountAsync();
        
        // Completed this week
        metrics.CompletedThisWeek = await context.Jobs
            .Where(j => j.CompletedAt.HasValue)
            .Where(j => j.CompletedAt >= weekStart)
            .CountAsync();
        
        // Jobs by status
        var statusCounts = await context.Jobs
            .GroupBy(j => j.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();
        
        metrics.DraftCount = statusCounts.FirstOrDefault(s => s.Status == JobStatus.Draft)?.Count ?? 0;
        metrics.ScheduledCount = statusCounts.FirstOrDefault(s => s.Status == JobStatus.Scheduled)?.Count ?? 0;
        metrics.InProgressCount = statusCounts.FirstOrDefault(s => s.Status == JobStatus.InProgress)?.Count ?? 0;
        metrics.CompletedCount = statusCounts.FirstOrDefault(s => s.Status == JobStatus.Completed)?.Count ?? 0;
        
        // Average job duration (completed jobs in last 30 days)
        var completedJobs = await context.Jobs
            .Where(j => j.Status == JobStatus.Completed)
            .Where(j => j.StartedAt.HasValue && j.CompletedAt.HasValue)
            .Where(j => j.CompletedAt >= today.AddDays(-30))
            .Select(j => new { j.StartedAt, j.CompletedAt })
            .ToListAsync();
        
        if (completedJobs.Any())
        {
            metrics.AverageJobDurationHours = (decimal)completedJobs
                .Average(j => (j.CompletedAt!.Value - j.StartedAt!.Value).TotalHours);
        }
        
        // Today's job value
        metrics.TodayJobValue = await context.Jobs
            .Where(j => j.ScheduledDate.HasValue && j.ScheduledDate.Value.Date == today)
            .SumAsync(j => (decimal?)j.Total) ?? 0;
        
        return metrics;
    }

    private async Task<CustomerMetrics> CalculateCustomerMetricsAsync(
        OneManVanDbContext context, DateTime today)
    {
        var metrics = new CustomerMetrics();
        
        metrics.TotalCustomers = await context.Customers.CountAsync();
        
        metrics.ActiveCustomers = await context.Customers
            .Where(c => c.Status == CustomerStatus.Active)
            .CountAsync();
        
        metrics.NewLast30Days = await context.Customers
            .Where(c => c.CreatedAt >= today.AddDays(-30))
            .CountAsync();
        
        metrics.NewLast7Days = await context.Customers
            .Where(c => c.CreatedAt >= today.AddDays(-7))
            .CountAsync();
        
        // Repeat customers (more than 1 job)
        metrics.RepeatCustomers = await context.Jobs
            .GroupBy(j => j.CustomerId)
            .Where(g => g.Count() > 1)
            .CountAsync();
        
        return metrics;
    }

    private async Task<ReceivablesMetrics> CalculateReceivablesAsync(
        OneManVanDbContext context, DateTime today)
    {
        var metrics = new ReceivablesMetrics();
        
        // Load data first, then calculate days overdue client-side (SQLite compatible)
        var unpaidInvoices = await context.Invoices
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft)
            .Select(i => new { 
                Balance = i.Total - i.AmountPaid, 
                DueDate = i.DueDate
            })
            .ToListAsync();
        
        // Calculate days overdue client-side
        var invoicesWithDays = unpaidInvoices.Select(i => new
        {
            i.Balance,
            i.DueDate,
            DaysOverdue = (today - i.DueDate).Days
        }).ToList();
        
        metrics.TotalOutstanding = invoicesWithDays.Sum(i => i.Balance);
        
        // Current (not yet due)
        metrics.Current = invoicesWithDays
            .Where(i => i.DaysOverdue <= 0)
            .Sum(i => i.Balance);
        
        // 1-30 days overdue
        metrics.Days1To30 = invoicesWithDays
            .Where(i => i.DaysOverdue >= 1 && i.DaysOverdue <= 30)
            .Sum(i => i.Balance);
        
        // 31-60 days overdue
        metrics.Days31To60 = invoicesWithDays
            .Where(i => i.DaysOverdue >= 31 && i.DaysOverdue <= 60)
            .Sum(i => i.Balance);
        
        // 61-90 days overdue
        metrics.Days61To90 = invoicesWithDays
            .Where(i => i.DaysOverdue >= 61 && i.DaysOverdue <= 90)
            .Sum(i => i.Balance);
        
        // 90+ days overdue
        metrics.Days90Plus = invoicesWithDays
            .Where(i => i.DaysOverdue > 90)
            .Sum(i => i.Balance);
        
        metrics.OverdueCount = invoicesWithDays.Count(i => i.DaysOverdue > 0);
        
        return metrics;
    }

    private async Task<EstimateMetrics> CalculateEstimateMetricsAsync(OneManVanDbContext context)
    {
        var metrics = new EstimateMetrics();
        
        var estimates = await context.Estimates
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Total = g.Sum(e => e.Total) })
            .ToListAsync();
        
        metrics.DraftCount = estimates.FirstOrDefault(e => e.Status == EstimateStatus.Draft)?.Count ?? 0;
        metrics.SentCount = estimates.FirstOrDefault(e => e.Status == EstimateStatus.Sent)?.Count ?? 0;
        metrics.AcceptedCount = estimates.FirstOrDefault(e => e.Status == EstimateStatus.Accepted)?.Count ?? 0;
        metrics.DeclinedCount = estimates.FirstOrDefault(e => e.Status == EstimateStatus.Declined)?.Count ?? 0;
        
        metrics.PendingValue = estimates
            .Where(e => e.Status == EstimateStatus.Sent)
            .Sum(e => e.Total);
        
        metrics.AcceptedValue = estimates
            .Where(e => e.Status == EstimateStatus.Accepted)
            .Sum(e => e.Total);
        
        // Conversion rate
        var totalSent = metrics.SentCount + metrics.AcceptedCount + metrics.DeclinedCount;
        if (totalSent > 0)
        {
            metrics.ConversionRate = (decimal)metrics.AcceptedCount / totalSent * 100;
        }
        
        return metrics;
    }

    private async Task<ServiceAgreementMetrics> CalculateServiceAgreementMetricsAsync(
        OneManVanDbContext context, DateTime today)
    {
        var metrics = new ServiceAgreementMetrics();
        
        // Use Date-only comparison to avoid time component issues
        var todayDate = today.Date;
        var in7Days = todayDate.AddDays(7);
        var in30Days = todayDate.AddDays(30);
        
        metrics.ActiveCount = await context.ServiceAgreements
            .Where(sa => sa.Status == AgreementStatus.Active)
            .CountAsync();
        
        // Expiring within 7 days (urgent)
        metrics.ExpiringSoonCount = await context.ServiceAgreements
            .Where(sa => sa.Status == AgreementStatus.Active)
            .Where(sa => sa.EndDate.Date >= todayDate && sa.EndDate.Date < in7Days)
            .CountAsync();
        
        // Expiring within 30 days (includes the 7-day urgent ones)
        metrics.ExpiringIn30Days = await context.ServiceAgreements
            .Where(sa => sa.Status == AgreementStatus.Active)
            .Where(sa => sa.EndDate.Date >= todayDate && sa.EndDate.Date < in30Days)
            .CountAsync();
        
        metrics.ExpiredCount = await context.ServiceAgreements
            .Where(sa => sa.EndDate.Date < todayDate)
            .CountAsync();
        
        // Monthly Recurring Revenue (MRR) from active agreements
        metrics.MonthlyRecurringRevenue = await context.ServiceAgreements
            .Where(sa => sa.Status == AgreementStatus.Active)
            .SumAsync(sa => sa.MonthlyPrice > 0 ? sa.MonthlyPrice : sa.AnnualPrice / 12);
        
        // Annual Contract Value
        metrics.AnnualContractValue = await context.ServiceAgreements
            .Where(sa => sa.Status == AgreementStatus.Active)
            .SumAsync(sa => sa.AnnualPrice);
        
        return metrics;
    }

    private async Task<InventoryMetrics> CalculateInventoryMetricsAsync(OneManVanDbContext context)
    {
        var metrics = new InventoryMetrics();
        
        metrics.TotalItems = await context.InventoryItems.CountAsync();
        
        metrics.LowStockCount = await context.InventoryItems
            .Where(i => i.QuantityOnHand <= i.ReorderPoint && i.ReorderPoint > 0)
            .CountAsync();
        
        metrics.OutOfStockCount = await context.InventoryItems
            .Where(i => i.QuantityOnHand == 0)
            .CountAsync();
        
        metrics.TotalInventoryValue = await context.InventoryItems
                    .SumAsync(i => i.QuantityOnHand * i.Cost);
        
                return metrics;
            }

            private async Task<EmployeeMetrics> CalculateEmployeeMetricsAsync(
                OneManVanDbContext context, DateTime today, DateTime weekStart)
            {
                var metrics = new EmployeeMetrics();
                var todayDate = today.Date;
                var weekStartDate = weekStart.Date;
        
                // Active employees
                metrics.ActiveEmployees = await context.Employees
                            .Where(e => e.Status == EmployeeStatus.Active)
                            .CountAsync();
        
                        // Hours logged today
                        metrics.HoursLoggedToday = await context.EmployeeTimeLogs
                            .Where(t => t.Date >= todayDate && t.Date < todayDate.AddDays(1))
                            .SumAsync(t => (decimal?)t.HoursWorked) ?? 0m;
        
                        // Hours logged this week
                        metrics.HoursLoggedThisWeek = await context.EmployeeTimeLogs
                            .Where(t => t.Date >= weekStartDate && t.Date <= todayDate)
                            .SumAsync(t => (decimal?)t.HoursWorked) ?? 0m;
        
                        // Currently clocked in (no ClockOut time)
                        metrics.ClockedInNow = await context.EmployeeTimeLogs
                            .Where(t => t.ClockIn.HasValue && t.ClockOut == null)
                            .CountAsync();
        
                        // Pending timesheets (unapproved entries from past week)
                        metrics.PendingTimesheets = await context.EmployeeTimeLogs
                            .Where(t => t.Date >= weekStartDate.AddDays(-7))
                            .Where(t => !t.IsApproved && t.ClockOut != null)
                            .CountAsync();
        
                        // Labor cost this week (hours * pay rate)
                        var weeklyLogs = await context.EmployeeTimeLogs
                            .Include(t => t.Employee)
                            .Where(t => t.Date >= weekStartDate && t.Date <= todayDate)
                            .ToListAsync();
        
                        metrics.LaborCostThisWeek = weeklyLogs
                            .Sum(t => t.HoursWorked * (t.Employee?.PayRate ?? 0m));
        
                        return metrics;
                    }
                }

                #region KPI Data Models

                public class DashboardKpis
                {
                    public RevenueMetrics Revenue { get; set; } = new();
                    public JobMetrics Jobs { get; set; } = new();
                    public CustomerMetrics Customers { get; set; } = new();
                    public ReceivablesMetrics Receivables { get; set; } = new();
    public EstimateMetrics Estimates { get; set; } = new();
    public ServiceAgreementMetrics ServiceAgreements { get; set; } = new();
    public InventoryMetrics Inventory { get; set; } = new();
    public EmployeeMetrics Employees { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class RevenueMetrics
{
    public decimal TodayRevenue { get; set; }
    public decimal YesterdayRevenue { get; set; }
    public decimal MonthRevenue { get; set; }
    public decimal LastMonthRevenue { get; set; }
    public decimal DayOverDayChange { get; set; }
    public decimal MonthOverMonthChange { get; set; }
    public decimal PaidThisMonth { get; set; }
    public decimal AverageDaysToPayment { get; set; }
}

public class JobMetrics
{
    public int TodayJobCount { get; set; }
    public int WeekJobCount { get; set; }
    public int CompletedThisWeek { get; set; }
    public int DraftCount { get; set; }
    public int ScheduledCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public decimal AverageJobDurationHours { get; set; }
    public decimal TodayJobValue { get; set; }
}

public class CustomerMetrics
{
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int NewLast30Days { get; set; }
    public int NewLast7Days { get; set; }
    public int RepeatCustomers { get; set; }
}

public class ReceivablesMetrics
{
    public decimal TotalOutstanding { get; set; }
    public decimal Current { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Days90Plus { get; set; }
    public int OverdueCount { get; set; }
}

public class EstimateMetrics
{
    public int DraftCount { get; set; }
    public int SentCount { get; set; }
    public int AcceptedCount { get; set; }
    public int DeclinedCount { get; set; }
    public decimal PendingValue { get; set; }
    public decimal AcceptedValue { get; set; }
    public decimal ConversionRate { get; set; }
}

public class ServiceAgreementMetrics
{
    public int ActiveCount { get; set; }
    public int ExpiringSoonCount { get; set; }  // Within 7 days
    public int ExpiringIn30Days { get; set; }
    public int ExpiredCount { get; set; }
    public decimal MonthlyRecurringRevenue { get; set; }
    public decimal AnnualContractValue { get; set; }
}

public class InventoryMetrics
{
    public int TotalItems { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public decimal TotalInventoryValue { get; set; }
}

public class EmployeeMetrics
{
    public int ActiveEmployees { get; set; }
    public decimal HoursLoggedToday { get; set; }
    public decimal HoursLoggedThisWeek { get; set; }
    public int ClockedInNow { get; set; }
    public int PendingTimesheets { get; set; }
    public decimal LaborCostThisWeek { get; set; }
}

#endregion
