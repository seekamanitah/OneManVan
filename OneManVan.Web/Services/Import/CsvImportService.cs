using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Web.Services.Import;

public class CsvImportService : ICsvImportService
{
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;
    private readonly ILogger<CsvImportService> _logger;

    public CsvImportService(
        IDbContextFactory<OneManVanDbContext> dbFactory,
        ILogger<CsvImportService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    #region Customer Import

    public async Task<ImportPreview<Customer>> PreviewCustomersFromCsvAsync(Stream csvStream)
    {
        var preview = new ImportPreview<Customer>();
        
        try
        {
            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, GetCsvConfiguration());
            
            var records = csv.GetRecords<CustomerImportDto>().ToList();
            preview.TotalRows = records.Count;
            
            await using var db = await _dbFactory.CreateDbContextAsync();
            var existingEmails = await db.Customers
                .Select(c => c.Email!.ToLower())
                .Where(e => e != null)
                .ToListAsync();
            
            foreach (var (record, index) in records.Select((r, i) => (r, i + 2))) // +2 for header row
            {
                var errors = ValidateCustomerDto(record, index);
                if (errors.Any())
                {
                    preview.ValidationErrors.AddRange(errors);
                    continue;
                }
                
                var customer = MapToCustomer(record);
                
                if (!string.IsNullOrEmpty(record.Email) && 
                    existingEmails.Contains(record.Email.ToLower()))
                {
                    preview.ExistingRecords.Add(customer);
                }
                else
                {
                    preview.NewRecords.Add(customer);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing customers from CSV");
            preview.ValidationErrors.Add(new ImportError 
            { 
                RowNumber = 0, 
                Message = $"Failed to parse CSV: {ex.Message}" 
            });
        }
        
        return preview;
    }

    public async Task<ImportResult> ImportCustomersFromCsvAsync(Stream csvStream, ImportOptions options)
    {
        var result = new ImportResult();
        
        try
        {
            // Reset stream position if possible
            if (csvStream.CanSeek)
                csvStream.Position = 0;
                
            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, GetCsvConfiguration());
            
            var records = csv.GetRecords<CustomerImportDto>().ToList();
            result.TotalRecords = records.Count;
            
            if (options.ValidateOnly)
            {
                var preview = await PreviewCustomersFromCsvAsync(csvStream);
                result.Errors = preview.ValidationErrors;
                result.Success = preview.IsValid;
                return result;
            }
            
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            // Get the next customer number for auto-generation
            var existingNumbers = await db.Customers
                .Where(c => c.CustomerNumber != null && c.CustomerNumber.StartsWith("C-"))
                .Select(c => c.CustomerNumber)
                .ToListAsync();
            
            var nextNumber = 1;
            if (existingNumbers.Any())
            {
                nextNumber = existingNumbers
                    .Select(n => int.TryParse(n?.Substring(2), out int num) ? num : 0)
                    .DefaultIfEmpty(0)
                    .Max() + 1;
            }
            
            foreach (var (record, index) in records.Select((r, i) => (r, i + 2)))
            {
                try
                {
                    var errors = ValidateCustomerDto(record, index);
                    if (errors.Any())
                    {
                        result.Errors.AddRange(errors);
                        result.ErrorCount++;
                        
                        if (options.StopOnError)
                            break;
                        continue;
                    }
                    
                    // Check for existing customer
                    Customer? existing = null;
                    if (!string.IsNullOrEmpty(record.Email))
                    {
                        existing = await db.Customers
                            .FirstOrDefaultAsync(c => c.Email!.ToLower() == record.Email.ToLower());
                    }
                    
                    if (existing != null)
                    {
                        if (options.UpdateExisting)
                        {
                            UpdateCustomer(existing, record);
                            result.UpdatedCount++;
                        }
                        else
                        {
                            result.SkippedCount++;
                        }
                    }
                    else
                    {
                        var customer = MapToCustomer(record);
                        
                        // Auto-generate customer number if not provided
                        if (string.IsNullOrEmpty(customer.CustomerNumber))
                        {
                            customer.CustomerNumber = $"C-{nextNumber:D4}";
                            nextNumber++;
                        }
                        
                        db.Customers.Add(customer);
                        result.ImportedCount++;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportError
                    {
                        RowNumber = index,
                        Message = ex.Message
                    });
                    result.ErrorCount++;
                    
                    if (options.StopOnError)
                        break;
                }
            }
            
            await db.SaveChangesAsync();
            result.Success = result.ErrorCount == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing customers from CSV");
            result.Errors.Add(new ImportError { RowNumber = 0, Message = ex.Message });
            result.Success = false;
        }
        
        return result;
    }

    private List<ImportError> ValidateCustomerDto(CustomerImportDto dto, int rowNumber)
    {
        var errors = new List<ImportError>();
        
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            errors.Add(new ImportError
            {
                RowNumber = rowNumber,
                Field = "Name",
                Message = "Name is required"
            });
        }
        
        if (!string.IsNullOrEmpty(dto.Email) && !IsValidEmail(dto.Email))
        {
            errors.Add(new ImportError
            {
                RowNumber = rowNumber,
                Field = "Email",
                Message = "Invalid email format",
                RawValue = dto.Email
            });
        }
        
        return errors;
    }

    private Customer MapToCustomer(CustomerImportDto dto)
    {
        return new Customer
        {
            Name = dto.Name ?? "Unknown",
            Email = dto.Email,
            Phone = dto.Phone,
            HomeAddress = dto.HomeAddress ?? dto.Address,
            Notes = dto.Notes,
            Status = ParseEnum<CustomerStatus>(dto.Status) ?? CustomerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    private void UpdateCustomer(Customer customer, CustomerImportDto dto)
    {
        if (!string.IsNullOrEmpty(dto.Name)) customer.Name = dto.Name;
        if (!string.IsNullOrEmpty(dto.Phone)) customer.Phone = dto.Phone;
        if (!string.IsNullOrEmpty(dto.HomeAddress ?? dto.Address)) 
            customer.HomeAddress = dto.HomeAddress ?? dto.Address;
        if (!string.IsNullOrEmpty(dto.Notes)) customer.Notes = dto.Notes;
        if (!string.IsNullOrEmpty(dto.Status))
        {
            var status = ParseEnum<CustomerStatus>(dto.Status);
            if (status.HasValue) customer.Status = status.Value;
        }
    }

    #endregion

    #region Product Import

    public async Task<ImportPreview<Product>> PreviewProductsFromCsvAsync(Stream csvStream)
    {
        var preview = new ImportPreview<Product>();
        
        try
        {
            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, GetCsvConfiguration());
            
            var records = csv.GetRecords<ProductImportDto>().ToList();
            preview.TotalRows = records.Count;
            
            await using var db = await _dbFactory.CreateDbContextAsync();
            var existingModels = await db.Products
                .Where(p => p.ModelNumber != null)
                .Select(p => p.ModelNumber.ToLower())
                .ToListAsync();
            
            foreach (var (record, index) in records.Select((r, i) => (r, i + 2)))
            {
                var errors = ValidateProductDto(record, index);
                if (errors.Any())
                {
                    preview.ValidationErrors.AddRange(errors);
                    continue;
                }
                
                var product = MapToProduct(record);
                
                if (!string.IsNullOrEmpty(record.ModelNumber) && 
                    existingModels.Contains(record.ModelNumber.ToLower()))
                {
                    preview.ExistingRecords.Add(product);
                }
                else
                {
                    preview.NewRecords.Add(product);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing products from CSV");
            preview.ValidationErrors.Add(new ImportError 
            { 
                RowNumber = 0, 
                Message = $"Failed to parse CSV: {ex.Message}" 
            });
        }
        
        return preview;
    }

    public async Task<ImportResult> ImportProductsFromCsvAsync(Stream csvStream, ImportOptions options)
    {
        var result = new ImportResult();
        
        try
        {
            if (csvStream.CanSeek) csvStream.Position = 0;
            
            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, GetCsvConfiguration());
            
            var records = csv.GetRecords<ProductImportDto>().ToList();
            result.TotalRecords = records.Count;
            
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            foreach (var (record, index) in records.Select((r, i) => (r, i + 2)))
            {
                try
                {
                    var errors = ValidateProductDto(record, index);
                    if (errors.Any())
                    {
                        result.Errors.AddRange(errors);
                        result.ErrorCount++;
                        if (options.StopOnError) break;
                        continue;
                    }
                    
                    Product? existing = null;
                    if (!string.IsNullOrEmpty(record.ModelNumber))
                    {
                        existing = await db.Products
                            .FirstOrDefaultAsync(p => p.ModelNumber.ToLower() == record.ModelNumber.ToLower());
                    }
                    
                    if (existing != null)
                    {
                        if (options.UpdateExisting)
                        {
                            UpdateProduct(existing, record);
                            result.UpdatedCount++;
                        }
                        else
                        {
                            result.SkippedCount++;
                        }
                    }
                    else
                    {
                        var product = MapToProduct(record);
                        db.Products.Add(product);
                        result.ImportedCount++;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportError { RowNumber = index, Message = ex.Message });
                    result.ErrorCount++;
                    if (options.StopOnError) break;
                }
            }
            
            await db.SaveChangesAsync();
            result.Success = result.ErrorCount == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing products from CSV");
            result.Errors.Add(new ImportError { RowNumber = 0, Message = ex.Message });
            result.Success = false;
        }
        
        return result;
    }

    private List<ImportError> ValidateProductDto(ProductImportDto dto, int rowNumber)
    {
        var errors = new List<ImportError>();
        
        if (string.IsNullOrWhiteSpace(dto.Manufacturer))
        {
            errors.Add(new ImportError
            {
                RowNumber = rowNumber,
                Field = "Manufacturer",
                Message = "Manufacturer is required"
            });
        }
        
        if (string.IsNullOrWhiteSpace(dto.ModelNumber))
        {
            errors.Add(new ImportError
            {
                RowNumber = rowNumber,
                Field = "ModelNumber",
                Message = "Model number is required"
            });
        }
        
        return errors;
    }

    private Product MapToProduct(ProductImportDto dto)
    {
        return new Product
        {
            ProductName = dto.Name,
            Manufacturer = dto.Manufacturer ?? "Unknown",
            ModelNumber = dto.ModelNumber ?? "Unknown",
            Description = dto.Description,
            SuggestedSellPrice = dto.Price,
            WholesaleCost = dto.Cost,
            Category = ParseEnum<ProductCategory>(dto.Category) ?? ProductCategory.Unknown,
            IsActive = dto.IsActive ?? true
        };
    }

    private void UpdateProduct(Product product, ProductImportDto dto)
    {
        if (!string.IsNullOrEmpty(dto.Name)) product.ProductName = dto.Name;
        if (!string.IsNullOrEmpty(dto.Description)) product.Description = dto.Description;
        if (dto.Price > 0) product.SuggestedSellPrice = dto.Price;
        if (dto.Cost > 0) product.WholesaleCost = dto.Cost;
        if (!string.IsNullOrEmpty(dto.Manufacturer)) product.Manufacturer = dto.Manufacturer;
        if (!string.IsNullOrEmpty(dto.ModelNumber)) product.ModelNumber = dto.ModelNumber;
        if (!string.IsNullOrEmpty(dto.Category)) 
        {
            var category = ParseEnum<ProductCategory>(dto.Category);
            if (category.HasValue) product.Category = category.Value;
        }
        if (dto.IsActive.HasValue) product.IsActive = dto.IsActive.Value;
    }

    #endregion

    #region Stub Implementations for Other Entities

    public Task<ImportPreview<Invoice>> PreviewInvoicesFromCsvAsync(Stream csvStream)
        => Task.FromResult(new ImportPreview<Invoice>());

    public Task<ImportResult> ImportInvoicesFromCsvAsync(Stream csvStream, ImportOptions options)
        => Task.FromResult(new ImportResult { Success = false, Errors = { new ImportError { Message = "Invoice import not yet implemented" } } });

    public Task<ImportPreview<Job>> PreviewJobsFromCsvAsync(Stream csvStream)
        => Task.FromResult(new ImportPreview<Job>());

    public Task<ImportResult> ImportJobsFromCsvAsync(Stream csvStream, ImportOptions options)
        => Task.FromResult(new ImportResult { Success = false, Errors = { new ImportError { Message = "Job import not yet implemented" } } });

    public Task<ImportPreview<Asset>> PreviewAssetsFromCsvAsync(Stream csvStream)
        => Task.FromResult(new ImportPreview<Asset>());

    public Task<ImportResult> ImportAssetsFromCsvAsync(Stream csvStream, ImportOptions options)
        => Task.FromResult(new ImportResult { Success = false, Errors = { new ImportError { Message = "Asset import not yet implemented" } } });

    public Task<ImportPreview<InventoryItem>> PreviewInventoryFromCsvAsync(Stream csvStream)
        => Task.FromResult(new ImportPreview<InventoryItem>());

    public Task<ImportResult> ImportInventoryFromCsvAsync(Stream csvStream, ImportOptions options)
        => Task.FromResult(new ImportResult { Success = false, Errors = { new ImportError { Message = "Inventory import not yet implemented" } } });

    public Task<ImportPreview<Estimate>> PreviewEstimatesFromCsvAsync(Stream csvStream)
        => Task.FromResult(new ImportPreview<Estimate>());

    public Task<ImportResult> ImportEstimatesFromCsvAsync(Stream csvStream, ImportOptions options)
        => Task.FromResult(new ImportResult { Success = false, Errors = { new ImportError { Message = "Estimate import not yet implemented" } } });

    public Task<ImportPreview<Company>> PreviewCompaniesFromCsvAsync(Stream csvStream)
        => Task.FromResult(new ImportPreview<Company>());

    public Task<ImportResult> ImportCompaniesFromCsvAsync(Stream csvStream, ImportOptions options)
        => Task.FromResult(new ImportResult { Success = false, Errors = { new ImportError { Message = "Company import not yet implemented" } } });

    public Task<ImportPreview<Site>> PreviewSitesFromCsvAsync(Stream csvStream)
        => Task.FromResult(new ImportPreview<Site>());

    public Task<ImportResult> ImportSitesFromCsvAsync(Stream csvStream, ImportOptions options)
        => Task.FromResult(new ImportResult { Success = false, Errors = { new ImportError { Message = "Site import not yet implemented" } } });

    public Task<ImportPreview<ServiceAgreement>> PreviewServiceAgreementsFromCsvAsync(Stream csvStream)
        => Task.FromResult(new ImportPreview<ServiceAgreement>());

    public Task<ImportResult> ImportServiceAgreementsFromCsvAsync(Stream csvStream, ImportOptions options)
        => Task.FromResult(new ImportResult { Success = false, Errors = { new ImportError { Message = "Service Agreement import not yet implemented" } } });

    public Task<ImportPreview<Employee>> PreviewEmployeesFromCsvAsync(Stream csvStream)
        => Task.FromResult(new ImportPreview<Employee>());

    public Task<ImportResult> ImportEmployeesFromCsvAsync(Stream csvStream, ImportOptions options)
        => Task.FromResult(new ImportResult { Success = false, Errors = { new ImportError { Message = "Employee import not yet implemented" } } });

    #endregion

    #region Helper Methods

    private CsvConfiguration GetCsvConfiguration()
    {
        return new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        };
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private T? ParseEnum<T>(string? value) where T : struct, Enum
    {
        if (string.IsNullOrEmpty(value)) return null;
        return Enum.TryParse<T>(value, true, out var result) ? result : null;
    }

    #endregion
}

#region Import DTOs

public class CustomerImportDto
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? HomeAddress { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
}

public class ProductImportDto
{
    public string? Name { get; set; }
    public string? SKU { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public string? Manufacturer { get; set; }
    public string? ModelNumber { get; set; }
    public string? Category { get; set; }
    public bool? IsActive { get; set; }
    public bool? RequiresSerialNumber { get; set; }
}

public class JobImportDto
{
    public string? Title { get; set; }
    public string? CustomerEmail { get; set; }
    public string? Description { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public decimal EstimatedCost { get; set; }
}

public class AssetImportDto
{
    public string? Name { get; set; }
    public string? CustomerEmail { get; set; }
    public string? SerialNumber { get; set; }
    public string? ModelNumber { get; set; }
    public string? Manufacturer { get; set; }
    public DateTime? InstallDate { get; set; }
    public DateTime? WarrantyExpiration { get; set; }
    public string? Location { get; set; }
}

public class InventoryImportDto
{
    public string? ProductSKU { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public string? Location { get; set; }
    public int? ReorderLevel { get; set; }
}

#endregion
