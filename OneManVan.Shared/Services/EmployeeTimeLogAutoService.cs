using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Shared.Services;

/// <summary>
/// Service for automatically creating employee time log entries from invoice labor items.
/// </summary>
public interface IEmployeeTimeLogAutoService
{
    Task CreateTimeLogsFromInvoiceAsync(int invoiceId);
    Task UpdateTimeLogsFromInvoiceAsync(int invoiceId);
}

public class EmployeeTimeLogAutoService : IEmployeeTimeLogAutoService
{
    private readonly IDbContextFactory<OneManVanDbContext> _contextFactory;

    public EmployeeTimeLogAutoService(IDbContextFactory<OneManVanDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Creates time log entries for all labor line items with employees on an invoice.
    /// </summary>
    public async Task CreateTimeLogsFromInvoiceAsync(int invoiceId)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();

        // Load invoice with related data
        var invoice = await db.Invoices
            .Include(i => i.LineItems)
            .Include(i => i.Job)
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null) return;

        // Get labor items with employees
        var laborItems = invoice.LineItems
            .Where(li => li.Source == "Labor" && li.SourceId.HasValue)
            .ToList();

        foreach (var laborItem in laborItems)
        {
            // Check if time log already exists for this line item
            var existingLog = await db.EmployeeTimeLogs
                .FirstOrDefaultAsync(tl => 
                    tl.InvoiceLineItemId == laborItem.Id);

            if (existingLog != null)
                continue; // Skip if already exists

            // Create new time log entry
            var timeLog = new EmployeeTimeLog
            {
                EmployeeId = laborItem.SourceId.Value,
                InvoiceId = invoiceId,
                InvoiceLineItemId = laborItem.Id,
                JobId = invoice.JobId,
                CustomerId = invoice.CustomerId,
                Date = invoice.InvoiceDate,
                ClockIn = invoice.InvoiceDate.AddHours(8), // Default 8 AM
                ClockOut = invoice.InvoiceDate.AddHours(8).AddHours((double)laborItem.Quantity),
                HoursWorked = laborItem.Quantity,
                HourlyRate = laborItem.UnitPrice,
                TotalPay = laborItem.Total,
                Description = laborItem.Description ?? "Invoice Labor",
                Notes = $"Auto-created from Invoice #{invoice.InvoiceNumber}",
                Source = "Invoice",
                CreatedAt = DateTime.UtcNow
            };

            db.EmployeeTimeLogs.Add(timeLog);
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Updates existing time logs when invoice labor items change.
    /// </summary>
    public async Task UpdateTimeLogsFromInvoiceAsync(int invoiceId)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();

        // Load invoice with related data
        var invoice = await db.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null) return;

        // Get all time logs for this invoice
        var existingTimeLogs = await db.EmployeeTimeLogs
            .Where(tl => tl.InvoiceId == invoiceId && tl.Source == "Invoice")
            .ToListAsync();

        // Get current labor items with employees
        var currentLaborItems = invoice.LineItems
            .Where(li => li.Source == "Labor" && li.SourceId.HasValue)
            .ToList();

        // Update or create time logs for current labor items
        foreach (var laborItem in currentLaborItems)
        {
            var existingLog = existingTimeLogs
                .FirstOrDefault(tl => tl.InvoiceLineItemId == laborItem.Id);

            if (existingLog != null)
            {
                // Update existing log
                existingLog.EmployeeId = laborItem.SourceId.Value;
                existingLog.Date = invoice.InvoiceDate;
                existingLog.ClockOut = existingLog.ClockIn?.AddHours((double)laborItem.Quantity) 
                    ?? invoice.InvoiceDate.AddHours(8).AddHours((double)laborItem.Quantity);
                existingLog.HoursWorked = laborItem.Quantity;
                existingLog.HourlyRate = laborItem.UnitPrice;
                existingLog.TotalPay = laborItem.Total;
                existingLog.Description = laborItem.Description ?? "Invoice Labor";
                existingLog.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new log
                var timeLog = new EmployeeTimeLog
                {
                    EmployeeId = laborItem.SourceId.Value,
                    InvoiceId = invoiceId,
                    InvoiceLineItemId = laborItem.Id,
                    JobId = invoice.JobId,
                    CustomerId = invoice.CustomerId,
                    Date = invoice.InvoiceDate,
                    ClockIn = invoice.InvoiceDate.AddHours(8),
                    ClockOut = invoice.InvoiceDate.AddHours(8).AddHours((double)laborItem.Quantity),
                    HoursWorked = laborItem.Quantity,
                    HourlyRate = laborItem.UnitPrice,
                    TotalPay = laborItem.Total,
                    Description = laborItem.Description ?? "Invoice Labor",
                    Notes = $"Auto-created from Invoice #{invoice.InvoiceNumber}",
                    Source = "Invoice",
                    CreatedAt = DateTime.UtcNow
                };

                db.EmployeeTimeLogs.Add(timeLog);
            }
        }

        // Remove time logs for deleted labor items
        var currentLineItemIds = currentLaborItems.Select(li => li.Id).ToHashSet();
        var logsToRemove = existingTimeLogs
            .Where(tl => tl.InvoiceLineItemId.HasValue && !currentLineItemIds.Contains(tl.InvoiceLineItemId.Value))
            .ToList();

        db.EmployeeTimeLogs.RemoveRange(logsToRemove);

        await db.SaveChangesAsync();
    }
}
