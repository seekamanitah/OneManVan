using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Shared.Services;

/// <summary>
/// Service for validating serial numbers across products and assets.
/// Prevents duplicate serial numbers and provides warnings when conflicts exist.
/// </summary>
public class SerialNumberValidator
{
    private readonly OneManVanDbContext _dbContext;

    public SerialNumberValidator(OneManVanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Checks if a serial number already exists in products or assets.
    /// </summary>
    public async Task<SerialNumberValidationResult> ValidateProductSerialAsync(
        string? serialNumber, 
        int? excludeProductId = null)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            return SerialNumberValidationResult.Valid();
        }

        var result = new SerialNumberValidationResult();

        // Check for duplicate in Products
        var existingProduct = await _dbContext.Products
            .Where(p => p.SerialNumber == serialNumber)
            .Where(p => !excludeProductId.HasValue || p.Id != excludeProductId.Value)
            .FirstOrDefaultAsync();

        if (existingProduct != null)
        {
            result.IsDuplicate = true;
            result.DuplicateType = "Product";
            result.DuplicateId = existingProduct.Id;
            result.DuplicateDescription = $"{existingProduct.Manufacturer} {existingProduct.ModelNumber}";
            result.Message = $"Serial number '{serialNumber}' already exists for product: {result.DuplicateDescription}";
            return result;
        }

        // Check for duplicate in Assets
        var existingAsset = await _dbContext.Assets
            .Include(a => a.Customer)
            .Where(a => a.Serial == serialNumber)
            .FirstOrDefaultAsync();

        if (existingAsset != null)
        {
            result.IsDuplicate = true;
            result.DuplicateType = "Asset";
            result.DuplicateId = existingAsset.Id;
            result.DuplicateDescription = $"{existingAsset.Brand} {existingAsset.Model}";
            result.DuplicateCustomer = existingAsset.Customer?.Name;
            result.Message = $"Serial number '{serialNumber}' already exists for asset: {result.DuplicateDescription}";
            
            if (!string.IsNullOrEmpty(result.DuplicateCustomer))
            {
                result.Message += $" (Customer: {result.DuplicateCustomer})";
            }
            
            return result;
        }

        result.IsValid = true;
        result.Message = "Serial number is available";
        return result;
    }

    /// <summary>
    /// Checks if a serial number already exists for an asset.
    /// </summary>
    public async Task<SerialNumberValidationResult> ValidateAssetSerialAsync(
        string? serialNumber, 
        int? excludeAssetId = null)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            return SerialNumberValidationResult.Valid();
        }

        var result = new SerialNumberValidationResult();

        // Check for duplicate in Assets
        var existingAsset = await _dbContext.Assets
            .Include(a => a.Customer)
            .Where(a => a.Serial == serialNumber)
            .Where(a => !excludeAssetId.HasValue || a.Id != excludeAssetId.Value)
            .FirstOrDefaultAsync();

        if (existingAsset != null)
        {
            result.IsDuplicate = true;
            result.DuplicateType = "Asset";
            result.DuplicateId = existingAsset.Id;
            result.DuplicateDescription = $"{existingAsset.Brand} {existingAsset.Model}";
            result.DuplicateCustomer = existingAsset.Customer?.Name;
            result.Message = $"Serial number '{serialNumber}' already exists for asset: {result.DuplicateDescription}";
            
            if (!string.IsNullOrEmpty(result.DuplicateCustomer))
            {
                result.Message += $" (Customer: {result.DuplicateCustomer})";
            }
            
            return result;
        }

        // Check for duplicate in Products (warn but don't block)
        var existingProduct = await _dbContext.Products
            .Where(p => p.SerialNumber == serialNumber)
            .FirstOrDefaultAsync();

        if (existingProduct != null)
        {
            result.IsValid = true;
            result.HasWarning = true;
            result.DuplicateType = "Product";
            result.DuplicateId = existingProduct.Id;
            result.DuplicateDescription = $"{existingProduct.Manufacturer} {existingProduct.ModelNumber}";
            result.Message = $"Note: Serial number matches product catalog item: {result.DuplicateDescription}";
            return result;
        }

        result.IsValid = true;
        result.Message = "Serial number is available";
        return result;
    }

    /// <summary>
    /// Gets a product by serial number.
    /// </summary>
    public async Task<Product?> GetProductBySerialAsync(string serialNumber)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
            return null;

        return await _dbContext.Products
            .FirstOrDefaultAsync(p => p.SerialNumber == serialNumber);
    }

    /// <summary>
    /// Gets an asset by serial number.
    /// </summary>
    public async Task<Asset?> GetAssetBySerialAsync(string serialNumber)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
            return null;

        return await _dbContext.Assets
            .Include(a => a.Customer)
            .Include(a => a.Site)
            .FirstOrDefaultAsync(a => a.Serial == serialNumber);
    }
}

/// <summary>
/// Result of serial number validation.
/// </summary>
public class SerialNumberValidationResult
{
    public bool IsValid { get; set; }
    public bool IsDuplicate { get; set; }
    public bool HasWarning { get; set; }
    public string Message { get; set; } = string.Empty;
    
    // Duplicate information
    public string? DuplicateType { get; set; }  // "Product" or "Asset"
    public int? DuplicateId { get; set; }
    public string? DuplicateDescription { get; set; }
    public string? DuplicateCustomer { get; set; }

    public static SerialNumberValidationResult Valid()
    {
        return new SerialNumberValidationResult
        {
            IsValid = true,
            Message = "Serial number is available"
        };
    }

    public static SerialNumberValidationResult Duplicate(string message)
    {
        return new SerialNumberValidationResult
        {
            IsValid = false,
            IsDuplicate = true,
            Message = message
        };
    }
}
