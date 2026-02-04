using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Services;

/// <summary>
/// Service for payroll calculations, pay period summaries, and tax reporting.
/// Supports weekly, bi-weekly, semi-monthly, and monthly pay periods.
/// </summary>
public interface IPayrollService
{
    Task<PayPeriodSummary> GetPayPeriodSummaryAsync(int employeeId, DateTime periodStart, DateTime periodEnd);
    Task<List<PayPeriodSummary>> GetAllEmployeesPayPeriodAsync(DateTime periodStart, DateTime periodEnd);
    Task<PayPeriodDates> GetCurrentPayPeriodAsync(PayFrequency frequency);
    Task<PayPeriodDates> GetPreviousPayPeriodAsync(PayFrequency frequency);
    Task<QuarterlyTaxSummary> GetQuarterlyTaxSummaryAsync(int year, int quarter);
    Task<AnnualTaxSummary> GetAnnualTaxSummaryAsync(int year);
    Task<EmployeePayment> CreatePaymentAsync(int employeeId, DateTime periodStart, DateTime periodEnd, string? notes = null);
    Task<bool> MarkTimeLogsPaidAsync(int employeeId, int paymentId, DateTime periodStart, DateTime periodEnd);
}

public class PayrollService : IPayrollService
{
    private readonly IDbContextFactory<OneManVanDbContext> _contextFactory;

    public PayrollService(IDbContextFactory<OneManVanDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Gets pay period summary for a single employee.
    /// Automatically calculates overtime when weekly hours exceed threshold (default 40).
    /// </summary>
    public async Task<PayPeriodSummary> GetPayPeriodSummaryAsync(int employeeId, DateTime periodStart, DateTime periodEnd)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var employee = await context.Employees.FindAsync(employeeId);
        if (employee == null)
            return new PayPeriodSummary { EmployeeId = employeeId };

        var timeLogs = await context.EmployeeTimeLogs
            .Include(t => t.Job)
            .Where(t => t.EmployeeId == employeeId)
            .Where(t => t.Date >= periodStart.Date && t.Date <= periodEnd.Date)
            .OrderBy(t => t.Date)
            .AsNoTracking()
            .ToListAsync();

        var summary = new PayPeriodSummary
        {
            EmployeeId = employeeId,
            EmployeeName = employee.DisplayName,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            HourlyRate = employee.PayRate,
            OvertimeRate = employee.PayRate * employee.OvertimeMultiplier,
            OvertimeThreshold = employee.OvertimeThresholdHours
        };

        // Calculate overtime automatically based on weekly hours exceeding threshold
        // Group time logs by work week (Monday-Sunday)
        var weeklyHours = CalculateWeeklyOvertimeHours(timeLogs, employee.OvertimeThresholdHours);
        
        summary.RegularHours = weeklyHours.RegularHours;
        summary.OvertimeHours = weeklyHours.OvertimeHours;
        summary.TotalHours = summary.RegularHours + summary.OvertimeHours;

        // Calculate pay
        summary.RegularPay = summary.RegularHours * summary.HourlyRate;
        summary.OvertimePay = summary.OvertimeHours * summary.OvertimeRate;
        summary.GrossPay = summary.RegularPay + summary.OvertimePay;

        // Days worked
        summary.DaysWorked = timeLogs.Select(t => t.Date.Date).Distinct().Count();

        // Unapproved time entries
        summary.UnapprovedEntries = timeLogs.Count(t => !t.IsApproved);

        // Unpaid time entries
        summary.UnpaidEntries = timeLogs.Count(t => !t.IsPaid);

        // Job breakdown - recalculate with proper overtime allocation
        summary.JobBreakdown = CalculateJobBreakdownWithOvertime(timeLogs, summary.HourlyRate, summary.OvertimeRate, employee.OvertimeThresholdHours);

        return summary;
    }

    /// <summary>
    /// Calculates regular vs overtime hours by grouping into work weeks (Mon-Sun).
    /// Hours exceeding the threshold in a week are counted as overtime.
    /// </summary>
    private (decimal RegularHours, decimal OvertimeHours) CalculateWeeklyOvertimeHours(
        List<EmployeeTimeLog> timeLogs, 
        int overtimeThreshold)
    {
        if (timeLogs.Count == 0)
            return (0, 0);

        // Group by work week (Monday start)
        var weeklyGroups = timeLogs.GroupBy(t => GetWeekStart(t.Date));

        decimal totalRegular = 0;
        decimal totalOvertime = 0;

        foreach (var week in weeklyGroups)
        {
            decimal weekHours = week.Sum(t => t.HoursWorked);
            
            if (weekHours <= overtimeThreshold)
            {
                totalRegular += weekHours;
            }
            else
            {
                totalRegular += overtimeThreshold;
                totalOvertime += weekHours - overtimeThreshold;
            }
        }

        return (totalRegular, totalOvertime);
    }

    /// <summary>
    /// Gets the Monday of the week for a given date.
    /// </summary>
    private DateTime GetWeekStart(DateTime date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek - 1 + 7) % 7;
        return date.Date.AddDays(-daysSinceMonday);
    }

    /// <summary>
    /// Calculates job breakdown with proper overtime allocation per week.
    /// </summary>
    private List<JobHoursSummary> CalculateJobBreakdownWithOvertime(
        List<EmployeeTimeLog> timeLogs, 
        decimal hourlyRate, 
        decimal overtimeRate, 
        int overtimeThreshold)
    {
        if (timeLogs.Count == 0)
            return new List<JobHoursSummary>();

        // For job breakdown, we'll use a proportional overtime allocation
        // First, calculate total overtime hours per week
        var weeklyOT = new Dictionary<DateTime, decimal>();
        foreach (var weekGroup in timeLogs.GroupBy(t => GetWeekStart(t.Date)))
        {
            decimal weekHours = weekGroup.Sum(t => t.HoursWorked);
            weeklyOT[weekGroup.Key] = Math.Max(0, weekHours - overtimeThreshold);
        }

        // Calculate job breakdown
        return timeLogs
            .GroupBy(t => new { t.JobId, JobTitle = t.Job?.Title ?? "No Job" })
            .Select(g =>
            {
                decimal jobHours = g.Sum(t => t.HoursWorked);
                
                // Proportionally allocate overtime based on when hours were worked
                decimal jobOvertimeHours = 0;
                decimal jobRegularHours = 0;

                foreach (var weekGroup in g.GroupBy(t => GetWeekStart(t.Date)))
                {
                    var weekStart = weekGroup.Key;
                    decimal weekOT = weeklyOT.GetValueOrDefault(weekStart, 0);
                    decimal weekTotalHours = timeLogs
                        .Where(t => GetWeekStart(t.Date) == weekStart)
                        .Sum(t => t.HoursWorked);

                    if (weekTotalHours > 0 && weekOT > 0)
                    {
                        // Proportionally allocate OT to this job based on its share of weekly hours
                        decimal jobWeekHours = weekGroup.Sum(t => t.HoursWorked);
                        decimal jobOTPortion = (jobWeekHours / weekTotalHours) * weekOT;
                        jobOvertimeHours += jobOTPortion;
                        jobRegularHours += jobWeekHours - jobOTPortion;
                    }
                    else
                    {
                        jobRegularHours += weekGroup.Sum(t => t.HoursWorked);
                    }
                }

                return new JobHoursSummary
                {
                    JobId = g.Key.JobId,
                    JobTitle = g.Key.JobTitle,
                    Hours = jobHours,
                    Pay = (jobRegularHours * hourlyRate) + (jobOvertimeHours * overtimeRate)
                };
            })
            .OrderByDescending(j => j.Hours)
            .ToList();
    }

    /// <summary>
    /// Gets pay period summaries for all active employees.
    /// </summary>
    public async Task<List<PayPeriodSummary>> GetAllEmployeesPayPeriodAsync(DateTime periodStart, DateTime periodEnd)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var activeEmployees = await context.Employees
            .Where(e => e.Status == EmployeeStatus.Active)
            .Select(e => e.Id)
            .ToListAsync();

        var summaries = new List<PayPeriodSummary>();
        foreach (var employeeId in activeEmployees)
        {
            var summary = await GetPayPeriodSummaryAsync(employeeId, periodStart, periodEnd);
            if (summary.TotalHours > 0)
                summaries.Add(summary);
        }

        return summaries.OrderBy(s => s.EmployeeName).ToList();
    }

    /// <summary>
    /// Gets the current pay period based on frequency.
    /// </summary>
    public Task<PayPeriodDates> GetCurrentPayPeriodAsync(PayFrequency frequency)
    {
        var today = DateTime.Today;
        return Task.FromResult(CalculatePayPeriod(today, frequency));
    }

    /// <summary>
    /// Gets the previous pay period based on frequency.
    /// </summary>
    public Task<PayPeriodDates> GetPreviousPayPeriodAsync(PayFrequency frequency)
    {
        var today = DateTime.Today;
        var current = CalculatePayPeriod(today, frequency);
        // Go back one day before current start to get previous period
        return Task.FromResult(CalculatePayPeriod(current.Start.AddDays(-1), frequency));
    }

    private PayPeriodDates CalculatePayPeriod(DateTime date, PayFrequency frequency)
    {
        return frequency switch
        {
            PayFrequency.Weekly => CalculateWeeklyPeriod(date),
            PayFrequency.BiWeekly => CalculateBiWeeklyPeriod(date),
            PayFrequency.SemiMonthly => CalculateSemiMonthlyPeriod(date),
            PayFrequency.Monthly => CalculateMonthlyPeriod(date),
            _ => CalculateWeeklyPeriod(date)
        };
    }

    private PayPeriodDates CalculateWeeklyPeriod(DateTime date)
    {
        // Week starts on Monday
        var daysSinceMonday = ((int)date.DayOfWeek - 1 + 7) % 7;
        var start = date.AddDays(-daysSinceMonday);
        return new PayPeriodDates(start, start.AddDays(6));
    }

    private PayPeriodDates CalculateBiWeeklyPeriod(DateTime date)
    {
        // Assume bi-weekly periods start from a known reference date (Jan 1, 2024)
        var reference = new DateTime(2024, 1, 1);
        var daysSinceReference = (date - reference).Days;
        var periodNumber = daysSinceReference / 14;
        var start = reference.AddDays(periodNumber * 14);
        return new PayPeriodDates(start, start.AddDays(13));
    }

    private PayPeriodDates CalculateSemiMonthlyPeriod(DateTime date)
    {
        // 1-15 and 16-end of month
        if (date.Day <= 15)
        {
            var start = new DateTime(date.Year, date.Month, 1);
            return new PayPeriodDates(start, new DateTime(date.Year, date.Month, 15));
        }
        else
        {
            var start = new DateTime(date.Year, date.Month, 16);
            var end = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
            return new PayPeriodDates(start, end);
        }
    }

    private PayPeriodDates CalculateMonthlyPeriod(DateTime date)
    {
        var start = new DateTime(date.Year, date.Month, 1);
        var end = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
        return new PayPeriodDates(start, end);
    }

    /// <summary>
    /// Gets quarterly tax summary for all employees.
    /// </summary>
    public async Task<QuarterlyTaxSummary> GetQuarterlyTaxSummaryAsync(int year, int quarter)
    {
        if (quarter < 1 || quarter > 4)
            throw new ArgumentException("Quarter must be between 1 and 4");

        var (startMonth, endMonth) = quarter switch
        {
            1 => (1, 3),
            2 => (4, 6),
            3 => (7, 9),
            4 => (10, 12),
            _ => (1, 3)
        };

        var periodStart = new DateTime(year, startMonth, 1);
        var periodEnd = new DateTime(year, endMonth, DateTime.DaysInMonth(year, endMonth));

        await using var context = await _contextFactory.CreateDbContextAsync();

        var payments = await context.EmployeePayments
            .Include(p => p.Employee)
            .Where(p => p.PayDate >= periodStart && p.PayDate <= periodEnd)
            .AsNoTracking()
            .ToListAsync();

        var timeLogs = await context.EmployeeTimeLogs
            .Where(t => t.Date >= periodStart && t.Date <= periodEnd)
            .AsNoTracking()
            .ToListAsync();

        return new QuarterlyTaxSummary
        {
            Year = year,
            Quarter = quarter,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalGrossWages = payments.Sum(p => p.GrossAmount),
            TotalNetPay = payments.Sum(p => p.NetAmount),
            TotalHoursWorked = timeLogs.Sum(t => t.HoursWorked),
            TotalRegularHours = timeLogs.Where(t => !t.IsOvertime).Sum(t => t.HoursWorked),
            TotalOvertimeHours = timeLogs.Where(t => t.IsOvertime).Sum(t => t.HoursWorked),
            EmployeeCount = payments.Select(p => p.EmployeeId).Distinct().Count(),
            PaymentCount = payments.Count,
            EmployeeSummaries = payments
                .GroupBy(p => new { p.EmployeeId, p.Employee?.DisplayName })
                .Select(g => new EmployeeTaxSummary
                {
                    EmployeeId = g.Key.EmployeeId,
                    EmployeeName = g.Key.DisplayName ?? "Unknown",
                    GrossWages = g.Sum(p => p.GrossAmount),
                    NetPay = g.Sum(p => p.NetAmount),
                    PaymentsReceived = g.Count()
                })
                .OrderBy(e => e.EmployeeName)
                .ToList()
        };
    }

    /// <summary>
    /// Gets annual tax summary for all employees (for W-2/1099 preparation).
    /// </summary>
    public async Task<AnnualTaxSummary> GetAnnualTaxSummaryAsync(int year)
    {
        var periodStart = new DateTime(year, 1, 1);
        var periodEnd = new DateTime(year, 12, 31);

        await using var context = await _contextFactory.CreateDbContextAsync();

        var employees = await context.Employees
            .Where(e => e.Status == EmployeeStatus.Active || e.TerminationDate >= periodStart)
            .AsNoTracking()
            .ToListAsync();

        var payments = await context.EmployeePayments
            .Where(p => p.PayDate >= periodStart && p.PayDate <= periodEnd)
            .AsNoTracking()
            .ToListAsync();

        var timeLogs = await context.EmployeeTimeLogs
            .Where(t => t.Date >= periodStart && t.Date <= periodEnd)
            .AsNoTracking()
            .ToListAsync();

        var summary = new AnnualTaxSummary
        {
            Year = year,
            TotalGrossWages = payments.Sum(p => p.GrossAmount),
            TotalNetPay = payments.Sum(p => p.NetAmount),
            TotalHoursWorked = timeLogs.Sum(t => t.HoursWorked),
            EmployeeCount = employees.Count,
            W2Employees = new List<EmployeeAnnualSummary>(),
            ContractorEmployees = new List<EmployeeAnnualSummary>()
        };

        foreach (var employee in employees)
        {
            var empPayments = payments.Where(p => p.EmployeeId == employee.Id).ToList();
            var empHours = timeLogs.Where(t => t.EmployeeId == employee.Id).Sum(t => t.HoursWorked);

            var empSummary = new EmployeeAnnualSummary
            {
                EmployeeId = employee.Id,
                EmployeeName = employee.DisplayName,
                EmployeeType = employee.Type,
                SSN = employee.TaxId,
                GrossWages = empPayments.Sum(p => p.GrossAmount),
                NetPay = empPayments.Sum(p => p.NetAmount),
                TotalHours = empHours,
                PaymentsReceived = empPayments.Count
            };

            if (employee.Type == EmployeeType.Contractor1099 || employee.Type == EmployeeType.SubcontractorCompany)
                summary.ContractorEmployees.Add(empSummary);
            else
                summary.W2Employees.Add(empSummary);
        }

        summary.W2Employees = summary.W2Employees.OrderBy(e => e.EmployeeName).ToList();
        summary.ContractorEmployees = summary.ContractorEmployees.OrderBy(e => e.EmployeeName).ToList();

        return summary;
    }

    /// <summary>
    /// Creates a payment record for an employee based on a pay period.
    /// </summary>
    public async Task<EmployeePayment> CreatePaymentAsync(int employeeId, DateTime periodStart, DateTime periodEnd, string? notes = null)
    {
        var summary = await GetPayPeriodSummaryAsync(employeeId, periodStart, periodEnd);

        await using var context = await _contextFactory.CreateDbContextAsync();

        var payment = new EmployeePayment
        {
            EmployeeId = employeeId,
            PayDate = DateTime.Today,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            RegularHours = summary.RegularHours,
            OvertimeHours = summary.OvertimeHours,
            RegularPay = summary.RegularPay,
            OvertimePay = summary.OvertimePay,
            GrossAmount = summary.GrossPay,
            NetAmount = summary.GrossPay, // Simplified - no deductions calculated yet
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        context.EmployeePayments.Add(payment);
        await context.SaveChangesAsync();

        // Mark time logs as paid
        await MarkTimeLogsPaidAsync(employeeId, payment.Id, periodStart, periodEnd);

        return payment;
    }

    /// <summary>
    /// Marks time logs for a period as paid.
    /// </summary>
    public async Task<bool> MarkTimeLogsPaidAsync(int employeeId, int paymentId, DateTime periodStart, DateTime periodEnd)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var timeLogs = await context.EmployeeTimeLogs
            .Where(t => t.EmployeeId == employeeId)
            .Where(t => t.Date >= periodStart.Date && t.Date <= periodEnd.Date)
            .Where(t => !t.IsPaid)
            .ToListAsync();

        foreach (var log in timeLogs)
        {
            log.IsPaid = true;
            log.PaymentId = paymentId;
        }

        await context.SaveChangesAsync();
        return true;
    }
}

#region DTOs

public enum PayFrequency
{
    Weekly,
    BiWeekly,
    SemiMonthly,
    Monthly
}

public record PayPeriodDates(DateTime Start, DateTime End);

public class PayPeriodSummary
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal OvertimeRate { get; set; }
    public decimal OvertimeThreshold { get; set; }
    public decimal RegularHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal TotalHours { get; set; }
    public decimal RegularPay { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal GrossPay { get; set; }
    public int DaysWorked { get; set; }
    public int UnapprovedEntries { get; set; }
    public int UnpaidEntries { get; set; }
    public List<JobHoursSummary> JobBreakdown { get; set; } = new();
}

public class JobHoursSummary
{
    public int? JobId { get; set; }
    public string JobTitle { get; set; } = "";
    public decimal Hours { get; set; }
    public decimal Pay { get; set; }
}

public class QuarterlyTaxSummary
{
    public int Year { get; set; }
    public int Quarter { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TotalGrossWages { get; set; }
    public decimal TotalNetPay { get; set; }
    public decimal TotalHoursWorked { get; set; }
    public decimal TotalRegularHours { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public int EmployeeCount { get; set; }
    public int PaymentCount { get; set; }
    public List<EmployeeTaxSummary> EmployeeSummaries { get; set; } = new();
}

public class EmployeeTaxSummary
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public decimal GrossWages { get; set; }
    public decimal NetPay { get; set; }
    public int PaymentsReceived { get; set; }
}

public class AnnualTaxSummary
{
    public int Year { get; set; }
    public decimal TotalGrossWages { get; set; }
    public decimal TotalNetPay { get; set; }
    public decimal TotalHoursWorked { get; set; }
    public int EmployeeCount { get; set; }
    public List<EmployeeAnnualSummary> W2Employees { get; set; } = new();
    public List<EmployeeAnnualSummary> ContractorEmployees { get; set; } = new();
}

public class EmployeeAnnualSummary
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public EmployeeType EmployeeType { get; set; }
    public string? SSN { get; set; }
    public decimal GrossWages { get; set; }
    public decimal NetPay { get; set; }
    public decimal TotalHours { get; set; }
    public int PaymentsReceived { get; set; }
}

#endregion
