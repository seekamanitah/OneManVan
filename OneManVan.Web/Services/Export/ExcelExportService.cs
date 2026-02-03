using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;

namespace OneManVan.Web.Services.Export;

public interface IExcelExportService
{
    Task<byte[]> ExportCustomersToExcelAsync();
    Task<byte[]> ExportInvoicesToExcelAsync();
    Task<byte[]> ExportJobsToExcelAsync();
    Task<byte[]> ExportAssetsToExcelAsync();
    Task<byte[]> ExportProductsToExcelAsync();
    Task<byte[]> ExportInventoryToExcelAsync();
    Task<byte[]> ExportEstimatesToExcelAsync();
    Task<byte[]> ExportCompaniesToExcelAsync();
    Task<byte[]> ExportSitesToExcelAsync();
    Task<byte[]> ExportServiceAgreementsToExcelAsync();
    Task<byte[]> ExportWarrantyClaimsToExcelAsync();
    Task<byte[]> ExportExpensesToExcelAsync();
}

public class ExcelExportService : IExcelExportService
{
    private readonly OneManVanDbContext _context;

    public ExcelExportService(OneManVanDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> ExportCustomersToExcelAsync()
    {
        var customers = await _context.Customers.OrderBy(c => c.Name).ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Customers");

        // Headers
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Name";
        worksheet.Cell(1, 3).Value = "Email";
        worksheet.Cell(1, 4).Value = "Phone";
        worksheet.Cell(1, 5).Value = "Address";
        worksheet.Cell(1, 6).Value = "Status";

        // Data
        for (int i = 0; i < customers.Count; i++)
        {
            var row = i + 2;
            worksheet.Cell(row, 1).Value = customers[i].Id;
            worksheet.Cell(row, 2).Value = customers[i].Name;
            worksheet.Cell(row, 3).Value = customers[i].Email;
            worksheet.Cell(row, 4).Value = customers[i].Phone;
            worksheet.Cell(row, 5).Value = customers[i].HomeAddress;
            worksheet.Cell(row, 6).Value = customers[i].Status.ToString();
        }

        StyleWorksheet(worksheet);
        return WorkbookToBytes(workbook);
    }

    public async Task<byte[]> ExportInvoicesToExcelAsync()
    {
        var invoices = await _context.Invoices
            .Include(i => i.Customer)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Invoices");

        // Headers
        worksheet.Cell(1, 1).Value = "Invoice #";
        worksheet.Cell(1, 2).Value = "Customer";
        worksheet.Cell(1, 3).Value = "Invoice Date";
        worksheet.Cell(1, 4).Value = "Due Date";
        worksheet.Cell(1, 5).Value = "Subtotal";
        worksheet.Cell(1, 6).Value = "Tax";
        worksheet.Cell(1, 7).Value = "Total";
        worksheet.Cell(1, 8).Value = "Status";

        // Data
        for (int i = 0; i < invoices.Count; i++)
        {
            var row = i + 2;
            worksheet.Cell(row, 1).Value = invoices[i].InvoiceNumber;
            worksheet.Cell(row, 2).Value = invoices[i].Customer?.Name;
            worksheet.Cell(row, 3).Value = invoices[i].InvoiceDate;
            worksheet.Cell(row, 4).Value = invoices[i].DueDate;
            worksheet.Cell(row, 5).Value = (double)invoices[i].SubTotal;
            worksheet.Cell(row, 6).Value = (double)invoices[i].TaxAmount;
            worksheet.Cell(row, 7).Value = (double)invoices[i].Total;
            worksheet.Cell(row, 8).Value = invoices[i].Status.ToString();
        }

        StyleWorksheet(worksheet);
        return WorkbookToBytes(workbook);
    }

    public async Task<byte[]> ExportJobsToExcelAsync()
    {
        var jobs = await _context.Jobs
            .Include(j => j.Customer)
            .OrderByDescending(j => j.ScheduledDate)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Jobs");

        //  Headers
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Job #";
        worksheet.Cell(1, 3).Value = "Customer";
        worksheet.Cell(1, 4).Value = "Title";
        worksheet.Cell(1, 5).Value = "Scheduled";
        worksheet.Cell(1, 6).Value = "Completed";
        worksheet.Cell(1, 7).Value = "Status";
        worksheet.Cell(1, 8).Value = "Priority";

        // Data
        for (int i = 0; i < jobs.Count; i++)
        {
            var row = i + 2;
            worksheet.Cell(row, 1).Value = jobs[i].Id;
            worksheet.Cell(row, 2).Value = jobs[i].JobNumber;
            worksheet.Cell(row, 3).Value = jobs[i].Customer?.Name;
            worksheet.Cell(row, 4).Value = jobs[i].Title;
            worksheet.Cell(row, 5).Value = jobs[i].ScheduledDate;
            worksheet.Cell(row, 6).Value = jobs[i].CompletedAt;
            worksheet.Cell(row, 7).Value = jobs[i].Status.ToString();
            worksheet.Cell(row, 8).Value = jobs[i].Priority.ToString();
        }

        StyleWorksheet(worksheet);
        return WorkbookToBytes(workbook);
    }

    public async Task<byte[]> ExportAssetsToExcelAsync()
    {
        var assets = await _context.Assets
            .Include(a => a.Customer)
            .Include(a => a.Site)
            .OrderBy(a => a.Customer!.Name)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Assets");

        // Headers
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Asset #";
        worksheet.Cell(1, 3).Value = "Asset Name";
        worksheet.Cell(1, 4).Value = "Customer";
        worksheet.Cell(1, 5).Value = "Site";
        worksheet.Cell(1, 6).Value = "Serial";
        worksheet.Cell(1, 7).Value = "Brand";
        worksheet.Cell(1, 8).Value = "Model";
        worksheet.Cell(1, 9).Value = "Equipment Type";
        worksheet.Cell(1, 10).Value = "Fuel Type";
        worksheet.Cell(1, 11).Value = "Tonnage";
        worksheet.Cell(1, 12).Value = "SEER2";
        worksheet.Cell(1, 13).Value = "Install Date";
        worksheet.Cell(1, 14).Value = "Status";

        // Data
        for (int i = 0; i < assets.Count; i++)
        {
            var row = i + 2;
            worksheet.Cell(row, 1).Value = assets[i].Id;
            worksheet.Cell(row, 2).Value = assets[i].AssetNumber;
            worksheet.Cell(row, 3).Value = assets[i].AssetName;
            worksheet.Cell(row, 4).Value = assets[i].Customer?.Name;
            worksheet.Cell(row, 5).Value = assets[i].Site?.SiteName;
            worksheet.Cell(row, 6).Value = assets[i].Serial;
            worksheet.Cell(row, 7).Value = assets[i].Brand;
            worksheet.Cell(row, 8).Value = assets[i].Model;
            worksheet.Cell(row, 9).Value = assets[i].EquipmentType.ToString();
            worksheet.Cell(row, 10).Value = assets[i].FuelType.ToString();
            worksheet.Cell(row, 11).Value = assets[i].TonnageX10.HasValue ? (assets[i].TonnageX10.Value / 10.0) : (double?)null;
            worksheet.Cell(row, 12).Value = assets[i].Seer2Rating.HasValue ? (double)assets[i].Seer2Rating.Value : (double?)null;
            worksheet.Cell(row, 13).Value = assets[i].InstallDate;
            worksheet.Cell(row, 14).Value = assets[i].Status.ToString();
        }

        StyleWorksheet(worksheet);
        return WorkbookToBytes(workbook);
    }

    public async Task<byte[]> ExportProductsToExcelAsync()
    {
        var products = await _context.Products
            .OrderBy(p => p.Manufacturer)
            .ThenBy(p => p.ModelNumber)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Products");

        // Headers
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Product #";
        worksheet.Cell(1, 3).Value = "Manufacturer";
        worksheet.Cell(1, 4).Value = "Model";
        worksheet.Cell(1, 5).Value = "Name";
        worksheet.Cell(1, 6).Value = "Description";
        worksheet.Cell(1, 7).Value = "Category";

        // Data
        for (int i = 0; i < products.Count; i++)
        {
            var row = i + 2;
            worksheet.Cell(row, 1).Value = products[i].Id;
            worksheet.Cell(row, 2).Value = products[i].ProductNumber;
            worksheet.Cell(row, 3).Value = products[i].Manufacturer;
            worksheet.Cell(row, 4).Value = products[i].ModelNumber;
            worksheet.Cell(row, 5).Value = products[i].ProductName;
            worksheet.Cell(row, 6).Value = products[i].Description;
            worksheet.Cell(row, 7).Value = products[i].Category.ToString();
        }

        StyleWorksheet(worksheet);
        return WorkbookToBytes(workbook);
    }

    public async Task<byte[]> ExportInventoryToExcelAsync()
    {
        var inventory = await _context.InventoryItems
            .Where(i => i.IsActive)
            .OrderBy(i => i.Name)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Inventory");

        // Headers
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "SKU";
        worksheet.Cell(1, 3).Value = "Name";
        worksheet.Cell(1, 4).Value = "Category";
        worksheet.Cell(1, 5).Value = "Qty on Hand";
        worksheet.Cell(1, 6).Value = "Reorder Point";
        worksheet.Cell(1, 7).Value = "Unit";
        worksheet.Cell(1, 8).Value = "Location";

        // Data
        for (int i = 0; i < inventory.Count; i++)
        {
            var row = i + 2;
            worksheet.Cell(row, 1).Value = inventory[i].Id;
            worksheet.Cell(row, 2).Value = inventory[i].Sku;
            worksheet.Cell(row, 3).Value = inventory[i].Name;
            worksheet.Cell(row, 4).Value = inventory[i].Category.ToString();
            worksheet.Cell(row, 5).Value = (double)inventory[i].QuantityOnHand;
            worksheet.Cell(row, 6).Value = (double)inventory[i].ReorderPoint;
            worksheet.Cell(row, 7).Value = inventory[i].Unit;
            worksheet.Cell(row, 8).Value = inventory[i].Location;
        }

        StyleWorksheet(worksheet);
        return WorkbookToBytes(workbook);
    }

    public async Task<byte[]> ExportEstimatesToExcelAsync()
    {
        var estimates = await _context.Estimates
            .Include(e => e.Customer)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Estimates");

        // Headers
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Customer";
        worksheet.Cell(1, 3).Value = "Title";
        worksheet.Cell(1, 4).Value = "Created";
        worksheet.Cell(1, 5).Value = "Expires";
        worksheet.Cell(1, 6).Value = "Subtotal";
        worksheet.Cell(1, 7).Value = "Tax";
        worksheet.Cell(1, 8).Value = "Total";
        worksheet.Cell(1, 9).Value = "Status";

        // Data
        for (int i = 0; i < estimates.Count; i++)
        {
            var row = i + 2;
            worksheet.Cell(row, 1).Value = estimates[i].Id;
            worksheet.Cell(row, 2).Value = estimates[i].Customer?.Name;
            worksheet.Cell(row, 3).Value = estimates[i].Title;
            worksheet.Cell(row, 4).Value = estimates[i].CreatedAt;
            worksheet.Cell(row, 5).Value = estimates[i].ExpiresAt;
            worksheet.Cell(row, 6).Value = (double)estimates[i].SubTotal;
            worksheet.Cell(row, 7).Value = (double)estimates[i].TaxAmount;
            worksheet.Cell(row, 8).Value = (double)estimates[i].Total;
            worksheet.Cell(row, 9).Value = estimates[i].Status.ToString();
        }

        StyleWorksheet(worksheet);
        return WorkbookToBytes(workbook);
    }

    public async Task<byte[]> ExportCompaniesToExcelAsync()
    {
        var companies = await _context.Companies
            .OrderBy(c => c.Name)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Companies");

        // Headers
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Name";
        worksheet.Cell(1, 3).Value = "Type";
        worksheet.Cell(1, 4).Value = "Website";
        worksheet.Cell(1, 5).Value = "Email";
        worksheet.Cell(1, 6).Value = "Phone";
        worksheet.Cell(1, 7).Value = "Active";

        // Data
        for (int i = 0; i < companies.Count; i++)
        {
            var row = i + 2;
            worksheet.Cell(row, 1).Value = companies[i].Id;
            worksheet.Cell(row, 2).Value = companies[i].Name;
            worksheet.Cell(row, 3).Value = companies[i].CompanyType.ToString();
            worksheet.Cell(row, 4).Value = companies[i].Website;
            worksheet.Cell(row, 5).Value = companies[i].Email;
            worksheet.Cell(row, 6).Value = companies[i].Phone;
            worksheet.Cell(row, 7).Value = companies[i].IsActive;
        }

        StyleWorksheet(worksheet);
        return WorkbookToBytes(workbook);
    }

    public async Task<byte[]> ExportSitesToExcelAsync()
    {
        var sites = await _context.Sites
            .Include(s => s.Company)
            .OrderBy(s => s.Company!.Name)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sites");

        // Headers
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Company";
        worksheet.Cell(1, 3).Value = "Site Name";
        worksheet.Cell(1, 4).Value = "Address";
        worksheet.Cell(1, 5).Value = "City";
        worksheet.Cell(1, 6).Value = "State";
        worksheet.Cell(1, 7).Value = "Zip";

        // Data
        for (int i = 0; i < sites.Count; i++)
        {
            var row = i + 2;
            worksheet.Cell(row, 1).Value = sites[i].Id;
            worksheet.Cell(row, 2).Value = sites[i].Company?.Name;
            worksheet.Cell(row, 3).Value = sites[i].SiteName;
            worksheet.Cell(row, 4).Value = sites[i].Address;
            worksheet.Cell(row, 5).Value = sites[i].City;
            worksheet.Cell(row, 6).Value = sites[i].State;
            worksheet.Cell(row, 7).Value = sites[i].ZipCode;
        }

        StyleWorksheet(worksheet);
        return WorkbookToBytes(workbook);
    }

    public async Task<byte[]> ExportServiceAgreementsToExcelAsync()
    {
        var agreements = await _context.ServiceAgreements
            .Include(sa => sa.Customer)
            .OrderBy(sa => sa.Customer!.Name)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Service Agreements");

        // Headers
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Customer";
        worksheet.Cell(1, 3).Value = "Agreement #";
        worksheet.Cell(1, 4).Value = "Start Date";
        worksheet.Cell(1, 5).Value = "End Date";
        worksheet.Cell(1, 6).Value = "Description";
        worksheet.Cell(1, 7).Value = "Active";

        // Data
        for (int i = 0; i < agreements.Count; i++)
        {
            var row = i + 2;
            worksheet.Cell(row, 1).Value = agreements[i].Id;
            worksheet.Cell(row, 2).Value = agreements[i].Customer?.Name;
            worksheet.Cell(row, 3).Value = agreements[i].AgreementNumber;
            worksheet.Cell(row, 4).Value = agreements[i].StartDate;
            worksheet.Cell(row, 5).Value = agreements[i].EndDate;
            worksheet.Cell(row, 6).Value = agreements[i].Description;
            worksheet.Cell(row, 7).Value = agreements[i].IsActive;
        }

        StyleWorksheet(worksheet);
        return WorkbookToBytes(workbook);
    }

    public async Task<byte[]> ExportWarrantyClaimsToExcelAsync()
    {
        var claims = await _context.WarrantyClaims
            .Include(wc => wc.Asset)
            .OrderByDescending(wc => wc.ClaimDate)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Warranty Claims");

        // Headers
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Claim Number";
        worksheet.Cell(1, 3).Value = "Asset";
        worksheet.Cell(1, 4).Value = "Claim Date";
        worksheet.Cell(1, 5).Value = "Issue";
        worksheet.Cell(1, 6).Value = "Status";
        worksheet.Cell(1, 7).Value = "Covered";
        worksheet.Cell(1, 8).Value = "Repair Cost";
        worksheet.Cell(1, 9).Value = "Customer Charge";
        worksheet.Cell(1, 10).Value = "Resolution";

        // Data
        for (int i = 0; i < claims.Count; i++)
        {
            var row = i + 2;
            worksheet.Cell(row, 1).Value = claims[i].Id;
            worksheet.Cell(row, 2).Value = claims[i].ClaimNumber;
            worksheet.Cell(row, 3).Value = claims[i].Asset?.AssetName;
            worksheet.Cell(row, 4).Value = claims[i].ClaimDate;
            worksheet.Cell(row, 5).Value = claims[i].IssueDescription;
            worksheet.Cell(row, 6).Value = claims[i].Status.ToString();
            worksheet.Cell(row, 7).Value = claims[i].IsCoveredByWarranty;
            worksheet.Cell(row, 8).Value = claims[i].RepairCost;
            worksheet.Cell(row, 9).Value = claims[i].CustomerCharge;
            worksheet.Cell(row, 10).Value = claims[i].Resolution;
        }

        StyleWorksheet(worksheet);
        return WorkbookToBytes(workbook);
    }

    public async Task<byte[]> ExportExpensesToExcelAsync()
    {
        var expenses = await _context.Expenses
            .Include(e => e.Employee)
            .Include(e => e.Job)
            .Include(e => e.Customer)
            .OrderByDescending(e => e.ExpenseDate)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Expenses");

        // Headers
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Expense #";
        worksheet.Cell(1, 3).Value = "Date";
        worksheet.Cell(1, 4).Value = "Category";
        worksheet.Cell(1, 5).Value = "Description";
        worksheet.Cell(1, 6).Value = "Amount";
        worksheet.Cell(1, 7).Value = "Tax";
        worksheet.Cell(1, 8).Value = "Total";
        worksheet.Cell(1, 9).Value = "Employee";
        worksheet.Cell(1, 10).Value = "Job #";
        worksheet.Cell(1, 11).Value = "Customer";
        worksheet.Cell(1, 12).Value = "Vendor";
        worksheet.Cell(1, 13).Value = "Status";
        worksheet.Cell(1, 14).Value = "Billable";
        worksheet.Cell(1, 15).Value = "Reimbursed";

        // Data
        for (int i = 0; i < expenses.Count; i++)
        {
            var row = i + 2;
            worksheet.Cell(row, 1).Value = expenses[i].Id;
            worksheet.Cell(row, 2).Value = expenses[i].ExpenseNumber;
            worksheet.Cell(row, 3).Value = expenses[i].ExpenseDate;
            worksheet.Cell(row, 4).Value = expenses[i].Category.ToString();
            worksheet.Cell(row, 5).Value = expenses[i].Description;
            worksheet.Cell(row, 6).Value = (double)expenses[i].Amount;
            worksheet.Cell(row, 7).Value = (double)expenses[i].TaxAmount;
            worksheet.Cell(row, 8).Value = (double)(expenses[i].Amount + expenses[i].TaxAmount);
            worksheet.Cell(row, 9).Value = expenses[i].Employee?.FullName;
            worksheet.Cell(row, 10).Value = expenses[i].Job?.JobNumber;
            worksheet.Cell(row, 11).Value = expenses[i].Customer?.Name;
            worksheet.Cell(row, 12).Value = expenses[i].VendorName;
            worksheet.Cell(row, 13).Value = expenses[i].Status.ToString();
            worksheet.Cell(row, 14).Value = expenses[i].IsBillable;
            worksheet.Cell(row, 15).Value = expenses[i].IsReimbursed;
        }

        StyleWorksheet(worksheet);
        return WorkbookToBytes(workbook);
    }

    #region Helper Methods

    private void StyleWorksheet(IXLWorksheet worksheet)
    {
        worksheet.Columns().AdjustToContents();
        worksheet.Row(1).Style.Font.Bold = true;
        worksheet.Row(1).Style.Fill.BackgroundColor = XLColor.LightBlue;
    }

    private byte[] WorkbookToBytes(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    #endregion
}
