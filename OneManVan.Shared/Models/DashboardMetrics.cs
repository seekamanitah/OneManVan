namespace OneManVan.Shared.Models;

/// <summary>
/// Dashboard metrics and business intelligence data.
/// </summary>
public class DashboardMetrics
{
    // ===== TODAY =====
    
    /// <summary>
    /// Total revenue from paid invoices today.
    /// </summary>
    public decimal TodayRevenue { get; set; }
    
    /// <summary>
    /// Number of jobs scheduled for today.
    /// </summary>
    public int TodayJobCount { get; set; }
    
    /// <summary>
    /// Number of jobs completed today.
    /// </summary>
    public int TodayCompletedJobs { get; set; }
    
    /// <summary>
    /// Number of currently active (in-progress) jobs.
    /// </summary>
    public int ActiveJobs { get; set; }
    
    /// <summary>
    /// Number of overdue jobs (past scheduled date, not completed).
    /// </summary>
    public int OverdueJobs { get; set; }
    
    // ===== THIS WEEK =====
    
    /// <summary>
    /// Total revenue this week.
    /// </summary>
    public decimal WeekRevenue { get; set; }
    
    /// <summary>
    /// Number of jobs this week.
    /// </summary>
    public int WeekJobCount { get; set; }
    
    /// <summary>
    /// Number of jobs completed this week.
    /// </summary>
    public int WeekCompletedJobs { get; set; }
    
    /// <summary>
    /// Number of new customers added this week.
    /// </summary>
    public int NewCustomersWeek { get; set; }
    
    // ===== THIS MONTH =====
    
    /// <summary>
    /// Total revenue this month.
    /// </summary>
    public decimal MonthRevenue { get; set; }
    
    /// <summary>
    /// Number of jobs this month.
    /// </summary>
    public int MonthJobCount { get; set; }
    
    /// <summary>
    /// Average job value this month.
    /// </summary>
    public decimal AvgJobValue { get; set; }
    
    /// <summary>
    /// Number of new customers this month.
    /// </summary>
    public int CustomerGrowth { get; set; }
    
    // ===== TRENDS =====
    
    /// <summary>
    /// Daily revenue for the last 7 days (for chart).
    /// </summary>
    public List<decimal> RevenueTrend { get; set; } = new();
    
    /// <summary>
    /// Job completion rate (percentage).
    /// </summary>
    public double CompletionRate { get; set; }
    
    /// <summary>
    /// Revenue percentage change vs. last week.
    /// </summary>
    public double RevenueChangePercent { get; set; }
    
    /// <summary>
    /// Jobs percentage change vs. last week.
    /// </summary>
    public double JobsChangePercent { get; set; }
    
    // ===== CALCULATED PROPERTIES =====
    
    /// <summary>
    /// Is revenue trending up?
    /// </summary>
    public bool IsRevenueTrendingUp => RevenueChangePercent > 0;
    
    /// <summary>
    /// Is jobs trending up?
    /// </summary>
    public bool IsJobsTrendingUp => JobsChangePercent > 0;
    
    /// <summary>
    /// Formatted trend text for revenue.
    /// </summary>
    public string RevenueTrendText
    {
        get
        {
            var symbol = RevenueChangePercent > 0 ? "?" : "?";
            var abs = Math.Abs(RevenueChangePercent);
            return $"{symbol} {abs:F1}% vs last week";
        }
    }
    
    /// <summary>
    /// Formatted trend text for jobs.
    /// </summary>
    public string JobsTrendText
    {
        get
        {
            var symbol = JobsChangePercent > 0 ? "?" : "?";
            var abs = Math.Abs(JobsChangePercent);
            return $"{symbol} {abs:F1}% vs last week";
        }
    }
}
