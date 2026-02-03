using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OneManVan.Web.Services.Export;
using OneManVan.Web.Services;
using OneManVan.Shared.Data;
using Microsoft.EntityFrameworkCore;
using OneManVan.Web.Services.Pdf;

namespace OneManVan.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ExportController : ControllerBase
{
    private readonly ICsvExportService _csvService;
    private readonly IExcelExportService _excelService;
    private readonly IInvoicePdfGenerator _invoicePdfGenerator;
    private readonly IEstimatePdfGenerator _estimatePdfGenerator;
    private readonly OneManVanDbContext _context;

    public ExportController(
        ICsvExportService csvService,
        IExcelExportService excelService,
        IInvoicePdfGenerator invoicePdfGenerator,
        IEstimatePdfGenerator estimatePdfGenerator,
        OneManVanDbContext context)
    {
        _csvService = csvService;
        _excelService = excelService;
        _invoicePdfGenerator = invoicePdfGenerator;
        _estimatePdfGenerator = estimatePdfGenerator;
        _context = context;
    }

    #region CSV Exports

    [HttpGet("customers/csv")]
    public async Task<IActionResult> ExportCustomersCsv()
    {
        var data = await _csvService.ExportCustomersToCsvAsync();
        return File(data, "text/csv", $"Customers_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("invoices/csv")]
    public async Task<IActionResult> ExportInvoicesCsv()
    {
        var data = await _csvService.ExportInvoicesToCsvAsync();
        return File(data, "text/csv", $"Invoices_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("jobs/csv")]
    public async Task<IActionResult> ExportJobsCsv()
    {
        var data = await _csvService.ExportJobsToCsvAsync();
        return File(data, "text/csv", $"Jobs_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("assets/csv")]
    public async Task<IActionResult> ExportAssetsCsv()
    {
        var data = await _csvService.ExportAssetsToCsvAsync();
        return File(data, "text/csv", $"Assets_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("products/csv")]
    public async Task<IActionResult> ExportProductsCsv()
    {
        var data = await _csvService.ExportProductsToCsvAsync();
        return File(data, "text/csv", $"Products_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("inventory/csv")]
    public async Task<IActionResult> ExportInventoryCsv()
    {
        var data = await _csvService.ExportInventoryToCsvAsync();
        return File(data, "text/csv", $"Inventory_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("estimates/csv")]
    public async Task<IActionResult> ExportEstimatesCsv()
    {
        var data = await _csvService.ExportEstimatesToCsvAsync();
        return File(data, "text/csv", $"Estimates_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("companies/csv")]
    public async Task<IActionResult> ExportCompaniesCsv()
    {
        var data = await _csvService.ExportCompaniesToCsvAsync();
        return File(data, "text/csv", $"Companies_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("sites/csv")]
    public async Task<IActionResult> ExportSitesCsv()
    {
        var data = await _csvService.ExportSitesToCsvAsync();
        return File(data, "text/csv", $"Sites_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("service-agreements/csv")]
    public async Task<IActionResult> ExportServiceAgreementsCsv()
    {
        var data = await _csvService.ExportServiceAgreementsToCsvAsync();
        return File(data, "text/csv", $"ServiceAgreements_{DateTime.Now:yyyyMMdd}.csv");
    }

    #endregion

    #region Excel Exports

    [HttpGet("customers/excel")]
    public async Task<IActionResult> ExportCustomersExcel()
    {
        var data = await _excelService.ExportCustomersToExcelAsync();
        return File(data, 
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Customers_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("invoices/excel")]
    public async Task<IActionResult> ExportInvoicesExcel()
    {
        var data = await _excelService.ExportInvoicesToExcelAsync();
        return File(data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Invoices_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("jobs/excel")]
    public async Task<IActionResult> ExportJobsExcel()
    {
        var data = await _excelService.ExportJobsToExcelAsync();
        return File(data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Jobs_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("assets/excel")]
    public async Task<IActionResult> ExportAssetsExcel()
    {
        var data = await _excelService.ExportAssetsToExcelAsync();
        return File(data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Assets_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("products/excel")]
    public async Task<IActionResult> ExportProductsExcel()
    {
        var data = await _excelService.ExportProductsToExcelAsync();
        return File(data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Products_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("inventory/excel")]
    public async Task<IActionResult> ExportInventoryExcel()
    {
        var data = await _excelService.ExportInventoryToExcelAsync();
        return File(data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Inventory_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("estimates/excel")]
    public async Task<IActionResult> ExportEstimatesExcel()
    {
        var data = await _excelService.ExportEstimatesToExcelAsync();
        return File(data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Estimates_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("companies/excel")]
    public async Task<IActionResult> ExportCompaniesExcel()
    {
        var data = await _excelService.ExportCompaniesToExcelAsync();
        return File(data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Companies_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("sites/excel")]
    public async Task<IActionResult> ExportSitesExcel()
    {
        var data = await _excelService.ExportSitesToExcelAsync();
        return File(data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Sites_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("service-agreements/excel")]
    public async Task<IActionResult> ExportServiceAgreementsExcel()
    {
        var data = await _excelService.ExportServiceAgreementsToExcelAsync();
        return File(data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"ServiceAgreements_{DateTime.Now:yyyyMMdd}.xlsx");
    }


    [HttpGet("warrantyclaims/csv")]
    public async Task<IActionResult> ExportWarrantyClaimsCsv()
    {
        var claims = await _context.WarrantyClaims
            .Include(c => c.Asset)
            .ThenInclude(a => a!.Customer)
            .OrderByDescending(c => c.ClaimDate)
            .AsNoTracking()
            .ToListAsync();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Id,ClaimNumber,AssetSerial,CustomerName,Status,ClaimDate,ResolvedDate,RepairCost,CustomerCharge");
        
        foreach (var claim in claims)
        {
            csv.AppendLine($"{claim.Id},{claim.ClaimNumber},{claim.Asset?.Serial ?? ""},{claim.Asset?.Customer?.Name ?? ""},{claim.Status},{claim.ClaimDate:yyyy-MM-dd},{claim.ResolvedDate?.ToString("yyyy-MM-dd") ?? ""},{claim.RepairCost},{claim.CustomerCharge}");
        }
        
        return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"WarrantyClaims_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("warrantyclaims/excel")]
    public async Task<IActionResult> ExportWarrantyClaimsExcel()
    {
        var claims = await _context.WarrantyClaims
            .Include(c => c.Asset)
            .ThenInclude(a => a!.Customer)
            .OrderByDescending(c => c.ClaimDate)
            .AsNoTracking()
            .ToListAsync();

        // For now, return CSV format as Excel (simple implementation)
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Id,ClaimNumber,AssetSerial,CustomerName,Status,ClaimDate,ResolvedDate,RepairCost,CustomerCharge,IssueDescription");
        
        foreach (var claim in claims)
        {
            csv.AppendLine($"{claim.Id},{claim.ClaimNumber},{claim.Asset?.Serial ?? ""},{claim.Asset?.Customer?.Name ?? ""},{claim.Status},{claim.ClaimDate:yyyy-MM-dd},{claim.ResolvedDate?.ToString("yyyy-MM-dd") ?? ""},{claim.RepairCost},{claim.CustomerCharge},\"{claim.IssueDescription?.Replace("\"", "'") ?? ""}\"");
        }
        
        return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), 
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"WarrantyClaims_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("documents/csv")]
    public async Task<IActionResult> ExportDocumentsCsv()
    {
        var docs = await _context.Documents
            .Where(d => d.IsActive)
            .OrderBy(d => d.Category)
            .ThenBy(d => d.Name)
            .AsNoTracking()
            .ToListAsync();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Id,Name,Category,Manufacturer,ModelNumber,EquipmentType,FileName,FileSize,Tags,CreatedAt,ViewCount");
        
        foreach (var doc in docs)
        {
            csv.AppendLine($"{doc.Id},\"{doc.Name?.Replace("\"", "'")}\",{doc.Category},{doc.Manufacturer ?? ""},{doc.ModelNumber ?? ""},{doc.EquipmentType ?? ""},{doc.FileName},{doc.FileSizeBytes},\"{doc.Tags?.Replace("\"", "'") ?? ""}\",{doc.CreatedAt:yyyy-MM-dd},{doc.ViewCount}");
        }
        
        return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"Documents_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("documents/excel")]
    public async Task<IActionResult> ExportDocumentsExcel()
    {
        var docs = await _context.Documents
            .Where(d => d.IsActive)
            .OrderBy(d => d.Category)
            .ThenBy(d => d.Name)
            .AsNoTracking()
            .ToListAsync();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Id,Name,Description,Category,Manufacturer,ModelNumber,EquipmentType,FileName,FileSize,ContentType,Tags,IsCustomDocument,IsFavorite,CreatedAt,ViewCount");
        
        foreach (var doc in docs)
        {
            csv.AppendLine($"{doc.Id},\"{doc.Name?.Replace("\"", "'")}\",\"{doc.Description?.Replace("\"", "'") ?? ""}\",{doc.Category},{doc.Manufacturer ?? ""},{doc.ModelNumber ?? ""},{doc.EquipmentType ?? ""},{doc.FileName},{doc.FileSizeBytes},{doc.ContentType},\"{doc.Tags?.Replace("\"", "'") ?? ""}\",{doc.IsCustomDocument},{doc.IsFavorite},{doc.CreatedAt:yyyy-MM-dd},{doc.ViewCount}");
        }
        
        return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), 
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Documents_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("employees/csv")]
    public async Task<IActionResult> ExportEmployeesCsv()
    {
        var employees = await _context.Employees
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .AsNoTracking()
            .ToListAsync();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Id,FirstName,LastName,Type,Status,StartDate,Phone,Email,PayRate,PayRateType,PaymentMethod");
        
        foreach (var emp in employees)
        {
            csv.AppendLine($"{emp.Id},{emp.FirstName},{emp.LastName},{emp.Type},{emp.Status},{emp.StartDate:yyyy-MM-dd},{emp.Phone ?? ""},{emp.Email ?? ""},{emp.PayRate},{emp.PayRateType},{emp.PaymentMethod}");
        }
        
        return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"Employees_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("employees/excel")]
    public async Task<IActionResult> ExportEmployeesExcel()
    {
        var employees = await _context.Employees
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .AsNoTracking()
            .ToListAsync();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Id,FirstName,LastName,Type,Status,StartDate,TerminationDate,Address,City,State,ZipCode,Phone,Email,PayRate,PayRateType,PaymentMethod,OvertimeMultiplier,PtoBalance,CompanyName,ServiceProvided,HasW4,HasI9,Has1099Setup,BackgroundCheckDate,BackgroundCheckResult,DrugTestDate,DrugTestResult");
        
        foreach (var emp in employees)
        {
            csv.AppendLine($"{emp.Id},{emp.FirstName},{emp.LastName},{emp.Type},{emp.Status},{emp.StartDate:yyyy-MM-dd},{emp.TerminationDate?.ToString("yyyy-MM-dd") ?? ""},{emp.Address ?? ""},{emp.City ?? ""},{emp.State ?? ""},{emp.ZipCode ?? ""},{emp.Phone ?? ""},{emp.Email ?? ""},{emp.PayRate},{emp.PayRateType},{emp.PaymentMethod},{emp.OvertimeMultiplier},{emp.PtoBalanceHours},{emp.CompanyName ?? ""},{emp.ServiceProvided ?? ""},{emp.HasW4},{emp.HasI9},{emp.Has1099Setup},{emp.BackgroundCheckDate?.ToString("yyyy-MM-dd") ?? ""},{emp.BackgroundCheckResult ?? ""},{emp.DrugTestDate?.ToString("yyyy-MM-dd") ?? ""},{emp.DrugTestResult ?? ""}");
        }
        
        return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), 
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Employees_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("expenses/csv")]
    public async Task<IActionResult> ExportExpensesCsv()
    {
        var data = await _csvService.ExportExpensesToCsvAsync();
        return File(data, "text/csv", $"Expenses_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("expenses/excel")]
    public async Task<IActionResult> ExportExpensesExcel()
    {
        var data = await _excelService.ExportExpensesToExcelAsync();
        return File(data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Expenses_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    #endregion

    #region PDF Exports

    [HttpGet("invoice/{id}/pdf")]
    public async Task<IActionResult> ExportInvoicePdf(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
            return NotFound();

        var data = _invoicePdfGenerator.GenerateInvoicePdf(invoice);
        return File(data, "application/pdf", $"Invoice_{invoice.InvoiceNumber}.pdf");
    }

    [HttpGet("estimate/{id}/pdf")]
    public async Task<IActionResult> ExportEstimatePdf(int id)
    {
        var estimate = await _context.Estimates
            .Include(e => e.Customer)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (estimate == null)
            return NotFound();

        var data = _estimatePdfGenerator.GenerateEstimatePdf(estimate);
        return File(data, "application/pdf", $"Estimate_{estimate.Title}.pdf");
    }

    [HttpGet("agreement/{id}/pdf")]
    public async Task<IActionResult> ExportAgreementPdf(int id, [FromServices] IServiceAgreementPdfGenerator pdfGenerator)
    {
        var agreement = await _context.ServiceAgreements
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (agreement == null)
            return NotFound();

        var data = pdfGenerator.GenerateAgreementPdf(agreement);
        return File(data, "application/pdf", $"ServiceAgreement_{agreement.AgreementNumber ?? agreement.Id.ToString()}.pdf");
    }

    #endregion

    #region Email

    [HttpPost("invoice/{id}/email")]
    public async Task<IActionResult> EmailInvoice(int id, [FromServices] IEmailService emailService)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
            return NotFound(new { message = "Invoice not found" });

        if (invoice.Customer?.Email == null)
            return BadRequest(new { message = "Customer does not have an email address" });

        if (!emailService.IsConfigured)
            return BadRequest(new { message = "Email service is not configured. Please configure SMTP settings in appsettings.json." });

        var pdfData = _invoicePdfGenerator.GenerateInvoicePdf(invoice);
        var success = await emailService.SendInvoiceEmailAsync(invoice.Customer.Email, invoice.InvoiceNumber, pdfData);

        if (success)
            return Ok(new { message = $"Invoice emailed to {invoice.Customer.Email}" });
        else
            return BadRequest(new { message = "Failed to send email. Please check email configuration." });
    }

    [HttpPost("estimate/{id}/email")]
    public async Task<IActionResult> EmailEstimate(int id, [FromServices] IEmailService emailService)
    {
        var estimate = await _context.Estimates
            .Include(e => e.Customer)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (estimate == null)
            return NotFound(new { message = "Estimate not found" });

        if (estimate.Customer?.Email == null)
            return BadRequest(new { message = "Customer does not have an email address" });

        if (!emailService.IsConfigured)
            return BadRequest(new { message = "Email service is not configured. Please configure SMTP settings in appsettings.json." });

        var pdfData = _estimatePdfGenerator.GenerateEstimatePdf(estimate);
        var success = await emailService.SendEstimateEmailAsync(estimate.Customer.Email, estimate.Title, pdfData);

        if (success)
            return Ok(new { message = $"Estimate emailed to {estimate.Customer.Email}" });
        else
            return BadRequest(new { message = "Failed to send email. Please check email configuration." });
    }

    #endregion

    #region Material Lists Export

    [HttpGet("material-lists/csv")]
    public async Task<IActionResult> ExportMaterialListsCsv()
    {
        var lists = await _context.MaterialLists
            .Include(ml => ml.Customer)
            .Include(ml => ml.Site)
            .Include(ml => ml.Items)
            .OrderByDescending(ml => ml.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Id,ListNumber,Title,Customer,Site,Status,Priority,ItemCount,MaterialCost,LaborTotal,TotalBidPrice,CreatedAt");
        
        foreach (var list in lists)
        {
            csv.AppendLine($"{list.Id},{list.ListNumber},\"{list.Title?.Replace("\"", "'")}\",\"{list.Customer?.DisplayName ?? ""}\",\"{list.Site?.SiteName ?? ""}\",{list.Status},{list.Priority},{list.Items.Count},{list.TotalMaterialCost:F2},{list.LaborTotal:F2},{list.TotalBidPrice:F2},{list.CreatedAt:yyyy-MM-dd}");
        }
        
        return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"MaterialLists_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("material-lists/excel")]
    public async Task<IActionResult> ExportMaterialListsExcel()
    {
        var lists = await _context.MaterialLists
            .Include(ml => ml.Customer)
            .Include(ml => ml.Site)
            .Include(ml => ml.Items)
            .OrderByDescending(ml => ml.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Id,ListNumber,Title,Customer,Site,SquareFootage,Zones,Stories,Status,Priority,ItemCount,MaterialCost,MarkupPercent,LaborTotal,DisposalFees,PermitCost,ContingencyAmount,TaxAmount,TotalBidPrice,CreatedAt");
        
        foreach (var list in lists)
        {
            csv.AppendLine($"{list.Id},{list.ListNumber},\"{list.Title?.Replace("\"", "'")}\",\"{list.Customer?.DisplayName ?? ""}\",\"{list.Site?.SiteName ?? ""}\",{list.SquareFootage ?? 0},{list.Zones},{list.Stories},{list.Status},{list.Priority},{list.Items.Count},{list.TotalMaterialCost:F2},{list.MarkupPercent:F2},{list.LaborTotal:F2},{list.DisposalFee + list.RefrigerantReclaimFee:F2},{list.PermitCost + list.InspectionFees:F2},{list.ContingencyAmount:F2},{list.TaxAmount:F2},{list.TotalBidPrice:F2},{list.CreatedAt:yyyy-MM-dd}");
        }
        
        return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), 
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"MaterialLists_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    #endregion
}
