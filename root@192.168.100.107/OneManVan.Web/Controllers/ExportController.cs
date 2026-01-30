using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OneManVan.Web.Services.Export;
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
    private readonly IInvoicePdfGenerator _pdfGenerator;
    private readonly OneManVanDbContext _context;

    public ExportController(
        ICsvExportService csvService,
        IExcelExportService excelService,
        IInvoicePdfGenerator pdfGenerator,
        OneManVanDbContext context)
    {
        _csvService = csvService;
        _excelService = excelService;
        _pdfGenerator = pdfGenerator;
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

        var data = _pdfGenerator.GenerateInvoicePdf(invoice);
        return File(data, "application/pdf", $"Invoice_{invoice.InvoiceNumber}.pdf");
    }

    #endregion
}
