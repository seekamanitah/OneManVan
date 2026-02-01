using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Services;

/// <summary>
/// Service for managing employees, contractors, time logs, and payments.
/// </summary>
public class EmployeeService
{
    private readonly IDbContextFactory<OneManVanDbContext> _contextFactory;

    public EmployeeService(IDbContextFactory<OneManVanDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    #region Employee CRUD

    public async Task<List<Employee>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Employees
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Employee>> GetByStatusAsync(EmployeeStatus status)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Employees
            .Where(e => e.Status == status)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Employee>> GetByTypeAsync(EmployeeType type)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Employees
            .Where(e => e.Type == type)
            .OrderBy(e => e.LastName)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Employee>> GetActiveAsync()
    {
        return await GetByStatusAsync(EmployeeStatus.Active);
    }

    public async Task<Employee?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Employees
            .Include(e => e.TimeLogs.OrderByDescending(t => t.Date).Take(10))
            .Include(e => e.PerformanceNotes.OrderByDescending(n => n.Date).Take(10))
            .Include(e => e.Payments.OrderByDescending(p => p.PayDate).Take(5))
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Employee> CreateAsync(Employee employee)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        employee.CreatedAt = DateTime.UtcNow;
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        return employee;
    }

    public async Task<Employee> UpdateAsync(Employee employee)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        employee.UpdatedAt = DateTime.UtcNow;
        context.Employees.Update(employee);
        await context.SaveChangesAsync();
        return employee;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var employee = await context.Employees.FindAsync(id);
        if (employee == null) return false;
        
        context.Employees.Remove(employee);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TerminateAsync(int id, DateTime terminationDate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var employee = await context.Employees.FindAsync(id);
        if (employee == null) return false;
        
        employee.Status = EmployeeStatus.Terminated;
        employee.TerminationDate = terminationDate;
        employee.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Time Logs

    public async Task<List<EmployeeTimeLog>> GetTimeLogsAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var query = context.EmployeeTimeLogs
            .Include(t => t.Job)
            .Where(t => t.EmployeeId == employeeId);

        if (startDate.HasValue)
            query = query.Where(t => t.Date >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(t => t.Date <= endDate.Value);

        return await query
            .OrderByDescending(t => t.Date)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<decimal> GetYtdHoursAsync(int employeeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var yearStart = new DateTime(DateTime.Now.Year, 1, 1);
        
        return await context.EmployeeTimeLogs
            .Where(t => t.EmployeeId == employeeId && t.Date >= yearStart)
            .SumAsync(t => t.HoursWorked);
    }

    public async Task<decimal> GetUnpaidHoursAsync(int employeeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.EmployeeTimeLogs
            .Where(t => t.EmployeeId == employeeId && !t.IsPaid)
            .SumAsync(t => t.HoursWorked);
    }

    public async Task<EmployeeTimeLog> LogTimeAsync(EmployeeTimeLog timeLog)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        timeLog.CreatedAt = DateTime.UtcNow;
        context.EmployeeTimeLogs.Add(timeLog);
        await context.SaveChangesAsync();
        return timeLog;
    }

    public async Task<EmployeeTimeLog> UpdateTimeLogAsync(EmployeeTimeLog timeLog)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.EmployeeTimeLogs.Update(timeLog);
        await context.SaveChangesAsync();
        return timeLog;
    }

    public async Task<bool> DeleteTimeLogAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var log = await context.EmployeeTimeLogs.FindAsync(id);
        if (log == null) return false;
        
        context.EmployeeTimeLogs.Remove(log);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ApproveTimeLogAsync(int id, string approvedBy)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var log = await context.EmployeeTimeLogs.FindAsync(id);
        if (log == null) return false;
        
        log.IsApproved = true;
        log.ApprovedBy = approvedBy;
        log.ApprovedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Performance Notes

    public async Task<List<EmployeePerformanceNote>> GetPerformanceNotesAsync(int employeeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.EmployeePerformanceNotes
            .Include(n => n.Job)
            .Where(n => n.EmployeeId == employeeId)
            .OrderByDescending(n => n.Date)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<EmployeePerformanceNote> AddPerformanceNoteAsync(EmployeePerformanceNote note)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        note.CreatedAt = DateTime.UtcNow;
        context.EmployeePerformanceNotes.Add(note);
        await context.SaveChangesAsync();
        return note;
    }

    public async Task<bool> DeletePerformanceNoteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var note = await context.EmployeePerformanceNotes.FindAsync(id);
        if (note == null) return false;
        
        context.EmployeePerformanceNotes.Remove(note);
        await context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Payments

    public async Task<List<EmployeePayment>> GetPaymentsAsync(int employeeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.EmployeePayments
            .Where(p => p.EmployeeId == employeeId)
            .OrderByDescending(p => p.PayDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<decimal> GetYtdPayAsync(int employeeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var yearStart = new DateTime(DateTime.Now.Year, 1, 1);
        
        return await context.EmployeePayments
            .Where(p => p.EmployeeId == employeeId && p.PayDate >= yearStart)
            .SumAsync(p => p.GrossAmount);
    }

    public async Task<EmployeePayment> RecordPaymentAsync(EmployeePayment payment, List<int>? timeLogIds = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        payment.CreatedAt = DateTime.UtcNow;
        context.EmployeePayments.Add(payment);
        await context.SaveChangesAsync();

        // Mark time logs as paid
        if (timeLogIds?.Any() == true)
        {
            var logs = await context.EmployeeTimeLogs
                .Where(t => timeLogIds.Contains(t.Id))
                .ToListAsync();

            foreach (var log in logs)
            {
                log.IsPaid = true;
                log.PaymentId = payment.Id;
            }
            await context.SaveChangesAsync();
        }

        // Update employee's last paid info
        var employee = await context.Employees.FindAsync(payment.EmployeeId);
        if (employee != null)
        {
            employee.LastPaidDate = payment.PayDate;
            employee.LastPaidAmount = payment.NetAmount;
            employee.LastPaidMethod = payment.PaymentMethod.ToString();
            employee.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        return payment;
    }

    #endregion

    #region Statistics

    public async Task<EmployeeStats> GetStatsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var employees = await context.Employees.ToListAsync();
        var yearStart = new DateTime(DateTime.Now.Year, 1, 1);
        
        return new EmployeeStats
        {
            TotalEmployees = employees.Count,
            ActiveEmployees = employees.Count(e => e.Status == EmployeeStatus.Active),
            FullTimeCount = employees.Count(e => e.Type == EmployeeType.FullTime && e.Status == EmployeeStatus.Active),
            PartTimeCount = employees.Count(e => e.Type == EmployeeType.PartTime && e.Status == EmployeeStatus.Active),
            ContractorCount = employees.Count(e => e.Type == EmployeeType.Contractor1099 && e.Status == EmployeeStatus.Active),
            SubcontractorCount = employees.Count(e => e.Type == EmployeeType.SubcontractorCompany && e.Status == EmployeeStatus.Active),
            YtdTotalPay = await context.EmployeePayments.Where(p => p.PayDate >= yearStart).SumAsync(p => p.GrossAmount),
            YtdTotalHours = await context.EmployeeTimeLogs.Where(t => t.Date >= yearStart).SumAsync(t => t.HoursWorked)
        };
    }

    #endregion
}

/// <summary>
/// Summary statistics for employees.
/// </summary>
public class EmployeeStats
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int FullTimeCount { get; set; }
    public int PartTimeCount { get; set; }
    public int ContractorCount { get; set; }
    public int SubcontractorCount { get; set; }
    public decimal YtdTotalPay { get; set; }
    public decimal YtdTotalHours { get; set; }
}
