using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneManVan.Web.Services.Import;

namespace OneManVan.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImportController : ControllerBase
{
    private readonly ICsvImportService _csvImportService;
    private readonly ILogger<ImportController> _logger;

    public ImportController(
        ICsvImportService csvImportService,
        ILogger<ImportController> logger)
    {
        _csvImportService = csvImportService;
        _logger = logger;
    }

    #region Customer Import

    [HttpPost("customers/preview")]
    public async Task<IActionResult> PreviewCustomers(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        if (!IsValidCsvFile(file))
            return BadRequest(new { error = "Invalid file type. Please upload a CSV file." });

        try
        {
            using var stream = file.OpenReadStream();
            var preview = await _csvImportService.PreviewCustomersFromCsvAsync(stream);
            return Ok(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing customers import");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("customers")]
    public async Task<IActionResult> ImportCustomers(
        IFormFile file,
        [FromQuery] bool updateExisting = false,
        [FromQuery] bool stopOnError = false)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        if (!IsValidCsvFile(file))
            return BadRequest(new { error = "Invalid file type. Please upload a CSV file." });

        try
        {
            var options = new ImportOptions
            {
                UpdateExisting = updateExisting,
                StopOnError = stopOnError
            };

            using var stream = file.OpenReadStream();
            var result = await _csvImportService.ImportCustomersFromCsvAsync(stream, options);
            
            if (result.Success)
                return Ok(result);
            else
                return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing customers");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    #endregion

    #region Product Import

    [HttpPost("products/preview")]
    public async Task<IActionResult> PreviewProducts(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        if (!IsValidCsvFile(file))
            return BadRequest(new { error = "Invalid file type. Please upload a CSV file." });

        try
        {
            using var stream = file.OpenReadStream();
            var preview = await _csvImportService.PreviewProductsFromCsvAsync(stream);
            return Ok(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing products import");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("products")]
    public async Task<IActionResult> ImportProducts(
        IFormFile file,
        [FromQuery] bool updateExisting = false,
        [FromQuery] bool stopOnError = false)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        if (!IsValidCsvFile(file))
            return BadRequest(new { error = "Invalid file type. Please upload a CSV file." });

        try
        {
            var options = new ImportOptions
            {
                UpdateExisting = updateExisting,
                StopOnError = stopOnError
            };

            using var stream = file.OpenReadStream();
            var result = await _csvImportService.ImportProductsFromCsvAsync(stream, options);
            
            if (result.Success)
                return Ok(result);
            else
                return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing products");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    #endregion

    #region Generic Endpoints for Other Entities

    [HttpPost("jobs")]
    public async Task<IActionResult> ImportJobs(IFormFile file, [FromQuery] bool updateExisting = false)
    {
        return await ImportEntity(file, updateExisting, 
            (s, o) => _csvImportService.ImportJobsFromCsvAsync(s, o));
    }

    [HttpPost("assets")]
    public async Task<IActionResult> ImportAssets(IFormFile file, [FromQuery] bool updateExisting = false)
    {
        return await ImportEntity(file, updateExisting, 
            (s, o) => _csvImportService.ImportAssetsFromCsvAsync(s, o));
    }

    [HttpPost("inventory")]
    public async Task<IActionResult> ImportInventory(IFormFile file, [FromQuery] bool updateExisting = false)
    {
        return await ImportEntity(file, updateExisting, 
            (s, o) => _csvImportService.ImportInventoryFromCsvAsync(s, o));
    }

    [HttpPost("estimates")]
    public async Task<IActionResult> ImportEstimates(IFormFile file, [FromQuery] bool updateExisting = false)
    {
        return await ImportEntity(file, updateExisting, 
            (s, o) => _csvImportService.ImportEstimatesFromCsvAsync(s, o));
    }

    [HttpPost("invoices")]
    public async Task<IActionResult> ImportInvoices(IFormFile file, [FromQuery] bool updateExisting = false)
    {
        return await ImportEntity(file, updateExisting, 
            (s, o) => _csvImportService.ImportInvoicesFromCsvAsync(s, o));
    }

    [HttpPost("companies")]
    public async Task<IActionResult> ImportCompanies(IFormFile file, [FromQuery] bool updateExisting = false)
    {
        return await ImportEntity(file, updateExisting, 
            (s, o) => _csvImportService.ImportCompaniesFromCsvAsync(s, o));
    }

    [HttpPost("sites")]
    public async Task<IActionResult> ImportSites(IFormFile file, [FromQuery] bool updateExisting = false)
    {
        return await ImportEntity(file, updateExisting, 
            (s, o) => _csvImportService.ImportSitesFromCsvAsync(s, o));
    }

    [HttpPost("agreements")]
    public async Task<IActionResult> ImportServiceAgreements(IFormFile file, [FromQuery] bool updateExisting = false)
    {
        return await ImportEntity(file, updateExisting, 
            (s, o) => _csvImportService.ImportServiceAgreementsFromCsvAsync(s, o));
    }

    [HttpPost("employees")]
    public async Task<IActionResult> ImportEmployees(IFormFile file, [FromQuery] bool updateExisting = false)
    {
        return await ImportEntity(file, updateExisting, 
            (s, o) => _csvImportService.ImportEmployeesFromCsvAsync(s, o));
    }

    #endregion

    #region Template Download

    [HttpGet("template/{entityType}")]
    [AllowAnonymous]
    public IActionResult DownloadTemplate(string entityType)
    {
        var template = entityType.ToLower() switch
        {
            "customers" => "Name,Email,Phone,HomeAddress,Status,Notes\nJohn Doe,john@example.com,555-1234,123 Main St,Active,Sample customer",
            "products" => "Name,SKU,Description,Price,Cost,Manufacturer,ModelNumber,Category,IsActive\nSample Product,SKU-001,Sample description,99.99,50.00,Acme Corp,MODEL-1,Equipment,true",
            "jobs" => "Title,CustomerEmail,Description,ScheduledDate,Status,Priority,EstimatedCost\nSample Job,john@example.com,Job description,2024-01-15,Scheduled,Normal,150.00",
            "assets" => "Name,CustomerEmail,SerialNumber,ModelNumber,Manufacturer,InstallDate,WarrantyExpiration,Location\nHVAC Unit,john@example.com,SN-12345,MODEL-A,Carrier,2023-06-15,2028-06-15,Rooftop",
            "inventory" => "ProductSKU,ProductName,Quantity,Location,ReorderLevel\nSKU-001,Sample Product,50,Warehouse A,10",
            _ => null
        };

        if (template == null)
            return NotFound(new { error = $"Unknown entity type: {entityType}" });

        var bytes = System.Text.Encoding.UTF8.GetBytes(template);
        return File(bytes, "text/csv", $"{entityType}-import-template.csv");
    }

    #endregion

    #region Helper Methods

    private bool IsValidImportFile(IFormFile file)
    {
        var validExtensions = new[] { ".csv", ".txt", ".xlsx", ".xls" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return validExtensions.Contains(extension);
    }

    private bool IsValidCsvFile(IFormFile file)
    {
        var validExtensions = new[] { ".csv", ".txt" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return validExtensions.Contains(extension);
    }

    private bool IsExcelFile(IFormFile file)
    {
        var excelExtensions = new[] { ".xlsx", ".xls" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return excelExtensions.Contains(extension);
    }

    private async Task<IActionResult> ImportEntity(
        IFormFile file,
        bool updateExisting,
        Func<Stream, ImportOptions, Task<ImportResult>> importFunc)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        if (!IsValidImportFile(file))
            return BadRequest(new { error = "Invalid file type. Please upload a CSV or Excel file." });

        try
        {
            var options = new ImportOptions { UpdateExisting = updateExisting };
            using var stream = file.OpenReadStream();
            var result = await importFunc(stream, options);
            
            if (result.Success)
                return Ok(result);
            else
                return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during import");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    #endregion
}
