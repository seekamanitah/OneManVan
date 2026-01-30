using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Shared.Services;

/// <summary>
/// Service for tracking equipment registration status and providing reminders.
/// </summary>
public class EquipmentRegistrationService
{
    private readonly OneManVanDbContext _dbContext;

    public EquipmentRegistrationService(OneManVanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Gets assets that need online registration.
    /// Includes assets installed within the last year that haven't been registered.
    /// </summary>
    public async Task<List<Asset>> GetUnregisteredAssetsAsync()
    {
        var oneYearAgo = DateTime.Today.AddYears(-1);

        return await _dbContext.Assets
            .Include(a => a.Customer)
            .Include(a => a.Site)
            .Where(a => !string.IsNullOrEmpty(a.Serial))  // Has serial number
            .Where(a => !a.IsRegisteredOnline)             // Not registered
            .Where(a => a.InstallDate.HasValue && a.InstallDate.Value >= oneYearAgo)  // Recent install
            .Where(a => a.WarrantyStartDate.HasValue)      // Has warranty
            .OrderByDescending(a => a.InstallDate)
            .ToListAsync();
    }

    /// <summary>
    /// Gets count of unregistered assets.
    /// </summary>
    public async Task<int> GetUnregisteredCountAsync()
    {
        var oneYearAgo = DateTime.Today.AddYears(-1);

        return await _dbContext.Assets
            .Where(a => !string.IsNullOrEmpty(a.Serial))
            .Where(a => !a.IsRegisteredOnline)
            .Where(a => a.InstallDate.HasValue && a.InstallDate.Value >= oneYearAgo)
            .Where(a => a.WarrantyStartDate.HasValue)
            .CountAsync();
    }

    /// <summary>
    /// Marks an asset as registered online.
    /// </summary>
    public async Task MarkAsRegisteredAsync(Asset asset, DateTime? registrationDate = null, string? confirmationNumber = null)
    {
        asset.IsRegisteredOnline = true;
        asset.RegistrationDate = registrationDate ?? DateTime.Today;
        asset.RegistrationConfirmation = confirmationNumber;

        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Gets the manufacturer registration URL for a brand from database.
    /// Falls back to hardcoded values if not in database (for compatibility).
    /// </summary>
    public async Task<string?> GetRegistrationUrlAsync(string? brand)
    {
        if (string.IsNullOrWhiteSpace(brand))
            return null;

        // Try to get from database first
        var manufacturer = await _dbContext.ManufacturerRegistrations
            .FirstOrDefaultAsync(m => m.BrandName.ToLower() == brand.ToLower() && m.IsActive);

        if (manufacturer != null)
            return manufacturer.RegistrationUrl;

        // Fallback to hardcoded (for backwards compatibility)
        return GetHardcodedRegistrationUrl(brand);
    }

    /// <summary>
    /// Gets all active manufacturer brands for dropdowns.
    /// </summary>
    public async Task<List<string>> GetActiveBrandsAsync()
    {
        return await _dbContext.ManufacturerRegistrations
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplayOrder)
            .Select(m => m.BrandName)
            .ToListAsync();
    }

    /// <summary>
    /// Gets manufacturer information including registration URL and notes.
    /// </summary>
    public async Task<ManufacturerRegistration?> GetManufacturerInfoAsync(string? brand)
    {
        if (string.IsNullOrWhiteSpace(brand))
            return null;

        return await _dbContext.ManufacturerRegistrations
            .FirstOrDefaultAsync(m => m.BrandName.ToLower() == brand.ToLower() && m.IsActive);
    }

    /// <summary>
    /// Fallback method for hardcoded URLs (backwards compatibility).
    /// </summary>
    private string? GetHardcodedRegistrationUrl(string brand)
    {
        if (string.IsNullOrWhiteSpace(brand))
            return null;

        return brand.ToLower() switch
        {
            "trane" => "https://www.trane.com/residential/en/register-your-product/",
            "carrier" => "https://www.carrier.com/residential/en/us/products/warranty-registration/",
            "lennox" => "https://www.lennox.com/owners/warranty-registration",
            "york" => "https://www.york.com/residential-equipment/resources/product-registration",
            "rheem" => "https://www.rheem.com/product-registration",
            "ruud" => "https://www.ruud.com/product-registration",
            "goodman" => "https://www.goodmanmfg.com/product-registration",
            "amana" => "https://www.amana-hac.com/en/support/product-registration",
            "bryant" => "https://www.bryant.com/en/us/products/warranty-registration/",
            "american standard" or "american-standard" => "https://www.americanstandardair.com/support/product-registration/",
            "daikin" => "https://www.daikincomfort.com/support/product-registration",
            "mitsubishi" => "https://www.mitsubishicomfort.com/products/warranty-registration",
            _ => null
        };
    }

    /// <summary>
    /// Checks if an asset needs registration (has serial, not registered, recent install).
    /// </summary>
    public bool NeedsRegistration(Asset asset)
    {
        if (string.IsNullOrEmpty(asset.Serial))
            return false;

        if (asset.IsRegisteredOnline)
            return false;

        if (!asset.InstallDate.HasValue)
            return false;

        var oneYearAgo = DateTime.Today.AddYears(-1);
        if (asset.InstallDate.Value < oneYearAgo)
            return false;

        if (!asset.WarrantyStartDate.HasValue)
            return false;

        return true;
    }
}
