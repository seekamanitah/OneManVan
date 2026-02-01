using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;

namespace OneManVan.Web.Services.Export;

public interface ICsvExportService
{
    Task<byte[]> ExportCustomersToCsvAsync();
    Task<byte[]> ExportInvoicesToCsvAsync();
    Task<byte[]> ExportJobsToCsvAsync();
    Task<byte[]> ExportAssetsToCsvAsync();
    Task<byte[]> ExportProductsToCsvAsync();
    Task<byte[]> ExportInventoryToCsvAsync();
    Task<byte[]> ExportEstimatesToCsvAsync();
    Task<byte[]> ExportCompaniesToCsvAsync();
    Task<byte[]> ExportSitesToCsvAsync();
    Task<byte[]> ExportServiceAgreementsToCsvAsync();
    Task<byte[]> ExportWarrantyClaimsToCsvAsync();
}

public class CsvExportService : ICsvExportService
{
    private readonly OneManVanDbContext _context;

    public CsvExportService(OneManVanDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> ExportCustomersToCsvAsync()
    {
        var customers = await _context.Customers
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Email,
                c.Phone,
                c.HomeAddress,
                Status = c.Status.ToString(),
                c.Notes
            })
            .ToListAsync();

        return ToCsv(customers);
    }

    public async Task<byte[]> ExportInvoicesToCsvAsync()
    {
        var invoices = await _context.Invoices
            .Include(i => i.Customer)
            .OrderByDescending(i => i.InvoiceDate)
            .Select(i => new
            {
                i.InvoiceNumber,
                CustomerName = i.Customer!.Name,
                i.InvoiceDate,
                i.DueDate,
                i.SubTotal,
                i.TaxAmount,
                i.Total,
                Status = i.Status.ToString()
            })
            .ToListAsync();

        return ToCsv(invoices);
    }

    public async Task<byte[]> ExportJobsToCsvAsync()
    {
        var jobs = await _context.Jobs
            .Include(j => j.Customer)
            .OrderByDescending(j => j.ScheduledDate)
            .Select(j => new
            {
                j.Id,
                j.JobNumber,
                CustomerName = j.Customer!.Name,
                j.Title,
                j.Description,
                j.ScheduledDate,
                j.CompletedAt,
                Status = j.Status.ToString(),
                Priority = j.Priority.ToString()
            })
            .ToListAsync();

        return ToCsv(jobs);
    }

    public async Task<byte[]> ExportAssetsToCsvAsync()
    {
        var assets = await _context.Assets
            .Include(a => a.Customer)
            .OrderBy(a => a.Customer!.Name)
            .Select(a => new
            {
                a.Id,
                CustomerName = a.Customer!.Name,
                a.AssetTag,
                a.Description,
                Status = a.Status.ToString(),
                a.InstallDate,
                a.Notes
            })
            .ToListAsync();

        return ToCsv(assets);
    }

    public async Task<byte[]> ExportProductsToCsvAsync()
    {
        var products = await _context.Products
            .OrderBy(p => p.Manufacturer)
            .ThenBy(p => p.ModelNumber)
            .Select(p => new
            {
                p.Id,
                p.ProductNumber,
                p.Manufacturer,
                p.ModelNumber,
                p.ProductName,
                p.Description,
                Category = p.Category.ToString()
            })
            .ToListAsync();

        return ToCsv(products);
    }

    public async Task<byte[]> ExportInventoryToCsvAsync()
    {
        var inventory = await _context.InventoryItems
            .Where(i => i.IsActive)
            .OrderBy(i => i.Name)
            .Select(i => new
            {
                i.Id,
                i.Sku,
                i.Name,
                i.Description,
                Category = i.Category.ToString(),
                i.QuantityOnHand,
                i.ReorderPoint,
                i.Unit,
                i.Location
            })
            .ToListAsync();

        return ToCsv(inventory);
    }

    public async Task<byte[]> ExportEstimatesToCsvAsync()
    {
        var estimates = await _context.Estimates
            .Include(e => e.Customer)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new
            {
                e.Id,
                CustomerName = e.Customer!.Name,
                e.Title,
                e.CreatedAt,
                e.ExpiresAt,
                e.SubTotal,
                e.TaxAmount,
                e.Total,
                Status = e.Status.ToString()
            })
            .ToListAsync();

        return ToCsv(estimates);
    }

    public async Task<byte[]> ExportCompaniesToCsvAsync()
    {
        var companies = await _context.Companies
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                Type = c.CompanyType.ToString(),
                c.Website,
                c.Email,
                c.Phone,
                c.IsActive
            })
            .ToListAsync();

        return ToCsv(companies);
    }

    public async Task<byte[]> ExportSitesToCsvAsync()
    {
        var sites = await _context.Sites
            .Include(s => s.Company)
            .OrderBy(s => s.Company!.Name)
            .ThenBy(s => s.SiteName)
            .Select(s => new
            {
                s.Id,
                CompanyName = s.Company!.Name,
                s.SiteName,
                s.Address,
                s.City,
                s.State,
                s.ZipCode
            })
            .ToListAsync();

        return ToCsv(sites);
    }

    public async Task<byte[]> ExportServiceAgreementsToCsvAsync()
    {
        var agreements = await _context.ServiceAgreements
            .Include(sa => sa.Customer)
            .OrderBy(sa => sa.Customer!.Name)
            .Select(sa => new
            {
                sa.Id,
                CustomerName = sa.Customer!.Name,
                sa.AgreementNumber,
                sa.StartDate,
                sa.EndDate,
                sa.Description,
                sa.IsActive
            })
            .ToListAsync();

        return ToCsv(agreements);
    }

    public async Task<byte[]> ExportWarrantyClaimsToCsvAsync()
    {
        var claims = await _context.WarrantyClaims
            .Include(wc => wc.Asset)
            .OrderByDescending(wc => wc.ClaimDate)
            .Select(wc => new
            {
                wc.Id,
                wc.ClaimNumber,
                AssetName = wc.Asset != null ? wc.Asset.AssetName : "",
                wc.ClaimDate,
                wc.IssueDescription,
                Status = wc.Status.ToString(),
                wc.IsCoveredByWarranty,
                wc.RepairCost,
                wc.CustomerCharge,
                wc.Resolution,
                wc.ResolvedDate
            })
            .ToListAsync();

        return ToCsv(claims);
    }

    private byte[] ToCsv<T>(IEnumerable<T> records)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        csv.WriteRecords(records);
        writer.Flush();

        return memoryStream.ToArray();
    }
}
