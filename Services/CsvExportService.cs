using System.IO;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OneManVan.ViewModels;

namespace OneManVan.Services;

/// <summary>
/// Service for exporting reports to CSV format.
/// </summary>
public class CsvExportService
{
    /// <summary>
    /// Exports the reports dashboard data to a CSV file.
    /// </summary>
    public async Task ExportReportToCsvAsync(ReportsViewModel viewModel, string filePath)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("OneManVan Business Report");
        sb.AppendLine($"Period,{viewModel.SelectedPeriod}");
        sb.AppendLine($"Generated,{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        // Revenue Metrics
        sb.AppendLine("REVENUE METRICS");
        sb.AppendLine("Metric,Value");
        sb.AppendLine($"Total Revenue,${viewModel.TotalRevenue:N2}");
        sb.AppendLine($"Paid,${viewModel.PaidRevenue:N2}");
        sb.AppendLine($"Outstanding,${viewModel.OutstandingRevenue:N2}");
        sb.AppendLine($"Average Job Value,${viewModel.AverageJobValue:N2}");
        sb.AppendLine();

        // Job Metrics
        sb.AppendLine("JOB METRICS");
        sb.AppendLine("Metric,Value");
        sb.AppendLine($"Total Jobs,{viewModel.TotalJobs}");
        sb.AppendLine($"Completed,{viewModel.CompletedJobs}");
        sb.AppendLine($"Cancelled,{viewModel.CancelledJobs}");
        sb.AppendLine($"Completion Rate,{viewModel.CompletionRate:N1}%");
        sb.AppendLine();

        // Customer Metrics
        sb.AppendLine("CUSTOMER METRICS");
        sb.AppendLine("Metric,Value");
        sb.AppendLine($"Total Customers,{viewModel.TotalCustomers}");
        sb.AppendLine($"New Customers,{viewModel.NewCustomers}");
        sb.AppendLine($"Active Customers,{viewModel.ActiveCustomers}");
        sb.AppendLine();

        // Asset Metrics
        sb.AppendLine("ASSET METRICS");
        sb.AppendLine("Metric,Value");
        sb.AppendLine($"Total Assets,{viewModel.TotalAssets}");
        sb.AppendLine($"Expiring Warranties (90 days),{viewModel.ExpiringWarranties}");
        sb.AppendLine();

        // Revenue by Fuel Type
        if (viewModel.RevenueByFuel.Count > 0)
        {
            sb.AppendLine("REVENUE BY FUEL TYPE");
            sb.AppendLine("Fuel Type,Job Count,Revenue");
            foreach (var item in viewModel.RevenueByFuel)
            {
                sb.AppendLine($"{EscapeCsvField(item.FuelType)},{item.JobCount},${item.Revenue:N2}");
            }
            sb.AppendLine();
        }

        // Jobs by Status
        if (viewModel.JobsByStatusData.Count > 0)
        {
            sb.AppendLine("JOBS BY STATUS");
            sb.AppendLine("Status,Count");
            foreach (var item in viewModel.JobsByStatusData)
            {
                sb.AppendLine($"{EscapeCsvField(item.Status)},{item.Count}");
            }
            sb.AppendLine();
        }

        // Top Customers
        if (viewModel.TopCustomers.Count > 0)
        {
            sb.AppendLine("TOP CUSTOMERS");
            sb.AppendLine("Customer Name,Job Count,Revenue");
            foreach (var item in viewModel.TopCustomers)
            {
                sb.AppendLine($"{EscapeCsvField(item.Name)},{item.JobCount},${item.Revenue:N2}");
            }
        }

        await File.WriteAllTextAsync(filePath, sb.ToString());
    }

    /// <summary>
    /// Exports detailed job data to CSV for further analysis.
    /// </summary>
    public async Task ExportJobsDetailToCsvAsync(string filePath)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Job ID,Customer,Site,Status,Scheduled Date,Completed Date,Total,Notes");

        var jobs = await App.DbContext.Jobs
            .Include(j => j.Customer)
            .Include(j => j.Site)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();

        foreach (var job in jobs)
        {
            sb.AppendLine(string.Join(",",
                job.Id,
                EscapeCsvField(job.Customer?.Name ?? "N/A"),
                EscapeCsvField(job.Site?.Address ?? "N/A"),
                job.Status,
                job.ScheduledDate?.ToString("yyyy-MM-dd") ?? "",
                job.CompletedAt?.ToString("yyyy-MM-dd") ?? "",
                $"${job.Total:N2}",
                EscapeCsvField(job.Notes ?? "")));
        }

        await File.WriteAllTextAsync(filePath, sb.ToString());
    }

    /// <summary>
    /// Exports customer data to CSV.
    /// </summary>
    public async Task ExportCustomersToCsvAsync(string filePath)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Customer ID,Name,Email,Phone,Created Date,Site Count,Asset Count");

        var customers = await App.DbContext.Customers
            .Include(c => c.Sites)
            .Include(c => c.Assets)
            .OrderBy(c => c.Name)
            .ToListAsync();

        foreach (var customer in customers)
        {
            sb.AppendLine(string.Join(",",
                customer.Id,
                EscapeCsvField(customer.Name),
                EscapeCsvField(customer.Email ?? ""),
                EscapeCsvField(customer.Phone ?? ""),
                customer.CreatedAt.ToString("yyyy-MM-dd"),
                customer.Sites.Count,
                customer.Assets.Count));
        }

        await File.WriteAllTextAsync(filePath, sb.ToString());
    }

    /// <summary>
    /// Exports asset data to CSV.
    /// </summary>
    public async Task ExportAssetsToCsvAsync(string filePath)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Asset ID,Serial,Brand,Model,Customer,Site,Fuel Type,Unit Config,BTU,SEER,Install Date,Warranty End,Days Until Expiry");

        var assets = await App.DbContext.Assets
            .Include(a => a.Customer)
            .Include(a => a.Site)
            .OrderBy(a => a.Customer.Name)
            .ThenBy(a => a.Serial)
            .ToListAsync();

        foreach (var asset in assets)
        {
            sb.AppendLine(string.Join(",",
                asset.Id,
                EscapeCsvField(asset.Serial),
                EscapeCsvField(asset.Brand ?? ""),
                EscapeCsvField(asset.Model ?? ""),
                EscapeCsvField(asset.Customer?.Name ?? "N/A"),
                EscapeCsvField(asset.Site?.Address ?? "N/A"),
                asset.FuelType,
                asset.UnitConfig,
                asset.BtuRating?.ToString() ?? "",
                asset.SeerRating?.ToString("N1") ?? "",
                asset.InstallDate?.ToString("yyyy-MM-dd") ?? "",
                asset.WarrantyEndDate?.ToString("yyyy-MM-dd") ?? "",
                asset.DaysUntilWarrantyExpires?.ToString() ?? ""));
        }

        await File.WriteAllTextAsync(filePath, sb.ToString());
    }

    /// <summary>
    /// Exports invoice data to CSV.
    /// </summary>
    public async Task ExportInvoicesToCsvAsync(string filePath)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Invoice ID,Invoice Number,Customer,Status,Invoice Date,Due Date,Subtotal,Tax,Total,Amount Paid,Balance");

        var invoices = await App.DbContext.Invoices
            .Include(i => i.Customer)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        foreach (var invoice in invoices)
        {
            sb.AppendLine(string.Join(",",
                invoice.Id,
                EscapeCsvField(invoice.InvoiceNumber),
                EscapeCsvField(invoice.Customer?.Name ?? "N/A"),
                invoice.Status,
                invoice.InvoiceDate.ToString("yyyy-MM-dd"),
                invoice.DueDate.ToString("yyyy-MM-dd"),
                $"${invoice.SubTotal:N2}",
                $"${invoice.TaxAmount:N2}",
                $"${invoice.Total:N2}",
                $"${invoice.AmountPaid:N2}",
                $"${invoice.Total - invoice.AmountPaid:N2}"));
        }

        await File.WriteAllTextAsync(filePath, sb.ToString());
    }

    /// <summary>
    /// Exports a generic list of objects to CSV.
    /// </summary>
    public async Task ExportToCsvAsync<T>(IEnumerable<T> data, string filePath)
    {
        var sb = new StringBuilder();
        var properties = typeof(T).GetProperties();

        // Header row
        sb.AppendLine(string.Join(",", properties.Select(p => EscapeCsvField(p.Name))));

        // Data rows
        foreach (var item in data)
        {
            var values = properties.Select(p =>
            {
                var value = p.GetValue(item);
                return EscapeCsvField(value?.ToString() ?? "");
            });
            sb.AppendLine(string.Join(",", values));
        }

        await File.WriteAllTextAsync(filePath, sb.ToString());
    }

    /// <summary>
    /// Escapes a field for CSV format.
    /// </summary>
    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        // If field contains comma, quote, or newline, wrap in quotes and escape quotes
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}
