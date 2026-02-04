using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Shared.Services;

/// <summary>
/// Service for managing employee time tracking on jobs.
/// Handles clock in/out, break tracking, and automatic EmployeeTimeLog creation.
/// </summary>
public interface IJobTimeClockService
{
    /// <summary>
    /// Assign an employee to a job.
    /// </summary>
    Task<JobWorker> AssignWorkerToJobAsync(int jobId, int employeeId, string? role = null, decimal? hourlyRateOverride = null);

    /// <summary>
    /// Remove an employee from a job.
    /// </summary>
    Task<bool> RemoveWorkerFromJobAsync(int jobId, int employeeId);

    /// <summary>
    /// Clock in an employee on a job.
    /// </summary>
    Task<TimeEntry> ClockInAsync(int jobId, int employeeId, TimeEntryType entryType = TimeEntryType.Labor, string? notes = null);

    /// <summary>
    /// Clock out an employee from a job.
    /// </summary>
    Task<TimeEntry?> ClockOutAsync(int jobId, int employeeId, string? notes = null);

    /// <summary>
    /// Start a break for an employee on a job.
    /// </summary>
    Task<TimeEntry> StartBreakAsync(int jobId, int employeeId);

    /// <summary>
    /// End a break and resume work.
    /// </summary>
    Task<TimeEntry?> EndBreakAsync(int jobId, int employeeId);

    /// <summary>
    /// Get all workers assigned to a job.
    /// </summary>
    Task<List<JobWorker>> GetJobWorkersAsync(int jobId);

    /// <summary>
    /// Get active time entries for a job.
    /// </summary>
    Task<List<TimeEntry>> GetActiveTimeEntriesAsync(int jobId);

    /// <summary>
    /// Calculate total hours worked on a job by all workers.
    /// </summary>
    Task<decimal> GetTotalJobHoursAsync(int jobId);

    /// <summary>
    /// Get time entries for an employee on a specific job.
    /// </summary>
    Task<List<TimeEntry>> GetEmployeeJobTimeEntriesAsync(int jobId, int employeeId);
}

public class JobTimeClockService : IJobTimeClockService
{
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;

    public JobTimeClockService(IDbContextFactory<OneManVanDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<JobWorker> AssignWorkerToJobAsync(int jobId, int employeeId, string? role = null, decimal? hourlyRateOverride = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Check if already assigned
        var existing = await db.JobWorkers
            .FirstOrDefaultAsync(jw => jw.JobId == jobId && jw.EmployeeId == employeeId);

        if (existing != null)
        {
            // Update existing assignment
            existing.Role = role ?? existing.Role;
            existing.HourlyRateOverride = hourlyRateOverride ?? existing.HourlyRateOverride;
            await db.SaveChangesAsync();
            return existing;
        }

        // Create new assignment
        var jobWorker = new JobWorker
        {
            JobId = jobId,
            EmployeeId = employeeId,
            Role = role,
            HourlyRateOverride = hourlyRateOverride,
            AssignedAt = DateTime.UtcNow
        };

        db.JobWorkers.Add(jobWorker);
        await db.SaveChangesAsync();

        return jobWorker;
    }

    public async Task<bool> RemoveWorkerFromJobAsync(int jobId, int employeeId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var jobWorker = await db.JobWorkers
            .FirstOrDefaultAsync(jw => jw.JobId == jobId && jw.EmployeeId == employeeId);

        if (jobWorker == null)
            return false;

        // Clock out if currently clocked in
        if (jobWorker.IsClockedIn && jobWorker.ActiveTimeEntryId.HasValue)
        {
            await ClockOutAsync(jobId, employeeId);
        }

        db.JobWorkers.Remove(jobWorker);
        await db.SaveChangesAsync();

        return true;
    }

    public async Task<TimeEntry> ClockInAsync(int jobId, int employeeId, TimeEntryType entryType = TimeEntryType.Labor, string? notes = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Ensure worker is assigned to job
        var jobWorker = await db.JobWorkers
            .Include(jw => jw.Employee)
            .FirstOrDefaultAsync(jw => jw.JobId == jobId && jw.EmployeeId == employeeId);

        if (jobWorker == null)
        {
            // Auto-assign if not already assigned
            jobWorker = new JobWorker
            {
                JobId = jobId,
                EmployeeId = employeeId,
                AssignedAt = DateTime.UtcNow
            };
            db.JobWorkers.Add(jobWorker);
        }

        // Check if already clocked in
        if (jobWorker.IsClockedIn && jobWorker.ActiveTimeEntryId.HasValue)
        {
            var activeEntry = await db.TimeEntries.FindAsync(jobWorker.ActiveTimeEntryId.Value);
            if (activeEntry != null)
                return activeEntry;
        }

        // Get employee for hourly rate
        var employee = await db.Employees.FindAsync(employeeId);
        var hourlyRate = jobWorker.HourlyRateOverride ?? employee?.PayRate ?? 0;

        // Create time entry
        var timeEntry = new TimeEntry
        {
            JobId = jobId,
            EmployeeId = employeeId,
            StartTime = DateTime.UtcNow,
            EntryType = entryType,
            HourlyRate = hourlyRate,
            IsBillable = entryType != TimeEntryType.Break,
            Notes = notes
        };

        db.TimeEntries.Add(timeEntry);
        await db.SaveChangesAsync();

        // Update job worker status
        jobWorker.IsClockedIn = true;
        jobWorker.ActiveTimeEntryId = timeEntry.Id;
        await db.SaveChangesAsync();

        return timeEntry;
    }

    public async Task<TimeEntry?> ClockOutAsync(int jobId, int employeeId, string? notes = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var jobWorker = await db.JobWorkers
            .Include(jw => jw.Employee)
            .FirstOrDefaultAsync(jw => jw.JobId == jobId && jw.EmployeeId == employeeId);

        if (jobWorker == null || !jobWorker.IsClockedIn || !jobWorker.ActiveTimeEntryId.HasValue)
            return null;

        var timeEntry = await db.TimeEntries.FindAsync(jobWorker.ActiveTimeEntryId.Value);
        if (timeEntry == null)
            return null;

        // Clock out the time entry
        timeEntry.EndTime = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(notes))
            timeEntry.Notes = string.IsNullOrEmpty(timeEntry.Notes) ? notes : $"{timeEntry.Notes}; {notes}";

        // Update totals on job worker
        var duration = (decimal)(timeEntry.EndTime.Value - timeEntry.StartTime).TotalHours;
        jobWorker.TotalHoursWorked += duration;
        jobWorker.TotalPayEarned += duration * timeEntry.HourlyRate;
        jobWorker.IsClockedIn = false;
        jobWorker.ActiveTimeEntryId = null;

        await db.SaveChangesAsync();

        // Create corresponding EmployeeTimeLog for payroll
        await CreateEmployeeTimeLogAsync(db, timeEntry, jobWorker.Employee);

        return timeEntry;
    }

    public async Task<TimeEntry> StartBreakAsync(int jobId, int employeeId)
    {
        // First clock out from current work entry
        await ClockOutAsync(jobId, employeeId, "Starting break");

        // Then clock in for break
        return await ClockInAsync(jobId, employeeId, TimeEntryType.Break, "On break");
    }

    public async Task<TimeEntry?> EndBreakAsync(int jobId, int employeeId)
    {
        // Clock out from break
        await ClockOutAsync(jobId, employeeId, "Break ended");

        // Clock back in for work
        return await ClockInAsync(jobId, employeeId, TimeEntryType.Labor, "Resumed work");
    }

    public async Task<List<JobWorker>> GetJobWorkersAsync(int jobId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.JobWorkers
            .Include(jw => jw.Employee)
            .Include(jw => jw.ActiveTimeEntry)
            .Where(jw => jw.JobId == jobId)
            .OrderBy(jw => jw.AssignedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<TimeEntry>> GetActiveTimeEntriesAsync(int jobId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.TimeEntries
            .Include(te => te.Employee)
            .Where(te => te.JobId == jobId && te.EndTime == null)
            .OrderBy(te => te.StartTime)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<decimal> GetTotalJobHoursAsync(int jobId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var entries = await db.TimeEntries
            .Where(te => te.JobId == jobId && te.EndTime != null)
            .ToListAsync();

        return entries.Sum(te => (decimal)(te.EndTime!.Value - te.StartTime).TotalHours);
    }

    public async Task<List<TimeEntry>> GetEmployeeJobTimeEntriesAsync(int jobId, int employeeId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.TimeEntries
            .Where(te => te.JobId == jobId && te.EmployeeId == employeeId)
            .OrderByDescending(te => te.StartTime)
            .AsNoTracking()
            .ToListAsync();
    }

    private async Task CreateEmployeeTimeLogAsync(OneManVanDbContext db, TimeEntry timeEntry, Employee? employee)
    {
        if (employee == null || timeEntry.EntryType == TimeEntryType.Break)
            return;

        var duration = timeEntry.EndTime.HasValue
            ? (decimal)(timeEntry.EndTime.Value - timeEntry.StartTime).TotalHours
            : 0;

        if (duration <= 0)
            return;

        // Check for overtime
        var isOvertime = false;
        if (employee.OvertimeThresholdHours > 0)
        {
            // Get total hours worked this week
            var weekStart = DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
            var weeklyHours = await db.EmployeeTimeLogs
                .Where(etl => etl.EmployeeId == employee.Id && etl.ClockIn >= weekStart)
                .SumAsync(etl => etl.HoursWorked);

            isOvertime = (weeklyHours + duration) > employee.OvertimeThresholdHours;
        }

        var hourlyRate = isOvertime
            ? employee.PayRate * employee.OvertimeMultiplier
            : employee.PayRate;

        var employeeTimeLog = new EmployeeTimeLog
        {
            EmployeeId = employee.Id,
            JobId = timeEntry.JobId,
            ClockIn = timeEntry.StartTime,
            ClockOut = timeEntry.EndTime,
            HoursWorked = duration,
            HourlyRate = hourlyRate,
            TotalPay = duration * hourlyRate,
            Source = "JobTimeClock",
            IsOvertime = isOvertime,
            Notes = $"Auto-logged from job time clock. {timeEntry.Notes}"
        };

        db.EmployeeTimeLogs.Add(employeeTimeLog);
        await db.SaveChangesAsync();
    }
}
