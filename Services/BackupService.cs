using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Services;

/// <summary>
/// Implementation of backup/restore functionality for OneManVan data.
/// </summary>
public class BackupService : IBackupService
{
    private readonly OneManVanDbContext _context;
    private readonly string _dbPath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public BackupService(OneManVanDbContext context, string dbPath)
    {
        _context = context;
        _dbPath = dbPath;
    }

    public string GetDatabasePath() => _dbPath;

    public async Task<string> ExportToJsonAsync(string filePath)
    {
        var backup = new BackupData
        {
            ExportedAt = DateTime.UtcNow,
            Version = "2.0",
            Customers = await _context.Customers.AsNoTracking().ToListAsync(),
            Sites = await _context.Sites.AsNoTracking().ToListAsync(),
            Assets = await _context.Assets.AsNoTracking().ToListAsync(),
            CustomFields = await _context.CustomFields.AsNoTracking().ToListAsync(),
            SchemaDefinitions = await _context.SchemaDefinitions.AsNoTracking().ToListAsync(),
            Estimates = await _context.Estimates.AsNoTracking().ToListAsync(),
            EstimateLines = await _context.EstimateLines.AsNoTracking().ToListAsync(),
            InventoryItems = await _context.InventoryItems.AsNoTracking().ToListAsync(),
            InventoryLogs = await _context.InventoryLogs.AsNoTracking().ToListAsync(),
            Jobs = await _context.Jobs.AsNoTracking().ToListAsync(),
            TimeEntries = await _context.TimeEntries.AsNoTracking().ToListAsync(),
            Invoices = await _context.Invoices.AsNoTracking().ToListAsync(),
            Payments = await _context.Payments.AsNoTracking().ToListAsync()
        };

        var json = JsonSerializer.Serialize(backup, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);

        return filePath;
    }

    public async Task<string> ExportToZipAsync(string filePath)
    {
        // Create a temporary JSON file
        var tempJsonPath = Path.Combine(Path.GetTempPath(), $"OneManVan_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        
        try
        {
            // Export to JSON first
            await ExportToJsonAsync(tempJsonPath);

            // Create ZIP with the JSON file
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using (var zipArchive = ZipFile.Open(filePath, ZipArchiveMode.Create))
            {
                zipArchive.CreateEntryFromFile(tempJsonPath, "backup.json", CompressionLevel.Optimal);
                
                // Add metadata file
                var metadataEntry = zipArchive.CreateEntry("metadata.txt");
                using (var writer = new StreamWriter(metadataEntry.Open()))
                {
                    await writer.WriteLineAsync($"OneManVan Backup");
                    await writer.WriteLineAsync($"Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    await writer.WriteLineAsync($"Version: 2.0");
                }
            }

            return filePath;
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempJsonPath))
            {
                File.Delete(tempJsonPath);
            }
        }
    }

    public async Task<ImportResult> ImportFromZipAsync(string filePath)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"OneManVan_Import_{Guid.NewGuid():N}");
        
        try
        {
            // Extract ZIP to temp directory
            Directory.CreateDirectory(tempDir);
            ZipFile.ExtractToDirectory(filePath, tempDir);

            // Find the JSON backup file
            var jsonFile = Path.Combine(tempDir, "backup.json");
            if (!File.Exists(jsonFile))
            {
                // Try to find any JSON file
                var jsonFiles = Directory.GetFiles(tempDir, "*.json");
                if (jsonFiles.Length == 0)
                {
                    return new ImportResult
                    {
                        Success = false,
                        Errors = ["No JSON backup file found in ZIP archive."]
                    };
                }
                jsonFile = jsonFiles[0];
            }

            // Import from JSON
            return await ImportFromJsonAsync(jsonFile);
        }
        finally
        {
            // Clean up temp directory
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    public async Task<string> ExportSubsetAsync(string filePath, ExportOptions options)
    {
        var backup = new BackupData
        {
            ExportedAt = DateTime.UtcNow,
            Version = "1.0"
        };

        if (options.IncludeCustomers)
        {
            var query = _context.Customers.AsNoTracking();
            if (options.FromDate.HasValue)
                query = query.Where(c => c.CreatedAt >= options.FromDate.Value);
            if (options.ToDate.HasValue)
                query = query.Where(c => c.CreatedAt <= options.ToDate.Value);
            backup.Customers = await query.ToListAsync();
        }

        if (options.IncludeSites)
        {
            var customerIds = backup.Customers.Select(c => c.Id).ToHashSet();
            backup.Sites = await _context.Sites
                .AsNoTracking()
                .Where(s => customerIds.Contains(s.CustomerId))
                .ToListAsync();
        }

        if (options.IncludeAssets)
        {
            var customerIds = backup.Customers.Select(c => c.Id).ToHashSet();
            backup.Assets = await _context.Assets
                .AsNoTracking()
                .Where(a => a.CustomerId.HasValue && customerIds.Contains(a.CustomerId.Value))
                .ToListAsync();
        }

        if (options.IncludeCustomFields)
        {
            var assetIds = backup.Assets.Select(a => a.Id).ToHashSet();
            backup.CustomFields = await _context.CustomFields
                .AsNoTracking()
                .Where(cf => cf.AssetId.HasValue && assetIds.Contains(cf.AssetId.Value))
                .ToListAsync();
        }

        var json = JsonSerializer.Serialize(backup, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);

        return filePath;
    }

    public async Task<ImportResult> ImportFromJsonAsync(string filePath)
    {
        var result = new ImportResult { Success = true };

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var backup = JsonSerializer.Deserialize<BackupData>(json, JsonOptions);

            if (backup == null)
            {
                result.Success = false;
                result.Errors.Add("Failed to parse backup file.");
                return result;
            }

            // Build ID mapping dictionaries for relinking foreign keys
            var customerIdMap = new Dictionary<int, int>(); // old ID -> new ID
            var siteIdMap = new Dictionary<int, int>();
            var assetIdMap = new Dictionary<int, int>();
            var estimateIdMap = new Dictionary<int, int>();
            var jobIdMap = new Dictionary<int, int>();
            var invoiceIdMap = new Dictionary<int, int>();
            var inventoryIdMap = new Dictionary<int, int>();

            // Import customers (upsert by name + email)
            foreach (var customer in backup.Customers)
            {
                var oldId = customer.Id;
                var existing = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Name == customer.Name && c.Email == customer.Email);

                if (existing != null)
                {
                    existing.Phone = customer.Phone;
                    existing.Notes = customer.Notes;
                    existing.LastServiceDate = customer.LastServiceDate;
                    customerIdMap[oldId] = existing.Id;
                    result.Warnings.Add($"Updated existing customer: {customer.Name}");
                }
                else
                {
                    customer.Id = 0;
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                    customerIdMap[oldId] = customer.Id;
                }
                result.CustomersImported++;
            }

            await _context.SaveChangesAsync();

            // Import sites
            foreach (var site in backup.Sites)
            {
                var oldId = site.Id;
                if (!customerIdMap.TryGetValue(site.CustomerId, out var newCustomerId))
                    continue;

                var existing = await _context.Sites
                    .FirstOrDefaultAsync(s => s.CustomerId == newCustomerId && s.Address == site.Address);

                if (existing == null)
                {
                    site.Id = 0;
                    site.CustomerId = newCustomerId;
                    _context.Sites.Add(site);
                    await _context.SaveChangesAsync();
                    siteIdMap[oldId] = site.Id;
                    result.SitesImported++;
                }
                else
                {
                    siteIdMap[oldId] = existing.Id;
                }
            }

            await _context.SaveChangesAsync();

            // Import assets (upsert by serial)
            foreach (var asset in backup.Assets)
            {
                var oldId = asset.Id;
                var existing = await _context.Assets
                    .FirstOrDefaultAsync(a => a.Serial == asset.Serial);

                if (existing != null)
                {
                    existing.Brand = asset.Brand;
                    existing.Model = asset.Model;
                    existing.FuelType = asset.FuelType;
                    existing.UnitConfig = asset.UnitConfig;
                    existing.BtuRating = asset.BtuRating;
                    existing.SeerRating = asset.SeerRating;
                    existing.Notes = asset.Notes;
                    assetIdMap[oldId] = existing.Id;
                    result.Warnings.Add($"Updated existing asset: {asset.Serial}");
                }
                else
                {
                    if (asset.CustomerId.HasValue && customerIdMap.TryGetValue(asset.CustomerId.Value, out var newCustomerId))
                    {
                        asset.Id = 0;
                        asset.CustomerId = newCustomerId;
                        asset.SiteId = asset.SiteId.HasValue && siteIdMap.TryGetValue(asset.SiteId.Value, out var newSiteId) 
                            ? newSiteId : null;
                        _context.Assets.Add(asset);
                        await _context.SaveChangesAsync();
                        assetIdMap[oldId] = asset.Id;
                    }
                }
                result.AssetsImported++;
            }

            await _context.SaveChangesAsync();

            // Import custom fields
            foreach (var field in backup.CustomFields)
            {
                field.Id = 0;
                if (field.AssetId.HasValue && assetIdMap.TryGetValue(field.AssetId.Value, out var newAssetId))
                {
                    field.AssetId = newAssetId;
                }
                _context.CustomFields.Add(field);
                result.CustomFieldsImported++;
            }

            await _context.SaveChangesAsync();

            // Import inventory items
            foreach (var item in backup.InventoryItems)
            {
                var oldId = item.Id;
                var existing = await _context.InventoryItems
                    .FirstOrDefaultAsync(i => i.Sku == item.Sku);

                if (existing != null)
                {
                    existing.Name = item.Name;
                    existing.Description = item.Description;
                    existing.QuantityOnHand = item.QuantityOnHand;
                    existing.Cost = item.Cost;
                    existing.Price = item.Price;
                    inventoryIdMap[oldId] = existing.Id;
                    result.Warnings.Add($"Updated existing inventory: {item.Name}");
                }
                else
                {
                    item.Id = 0;
                    _context.InventoryItems.Add(item);
                    await _context.SaveChangesAsync();
                    inventoryIdMap[oldId] = item.Id;
                }
                result.InventoryImported++;
            }

            await _context.SaveChangesAsync();

            // Import estimates
            foreach (var estimate in backup.Estimates)
            {
                var oldId = estimate.Id;
                if (!customerIdMap.TryGetValue(estimate.CustomerId, out var newCustomerId))
                    continue;

                estimate.Id = 0;
                estimate.CustomerId = newCustomerId;
                estimate.SiteId = estimate.SiteId.HasValue && siteIdMap.TryGetValue(estimate.SiteId.Value, out var newSiteId) 
                    ? newSiteId : null;
                estimate.AssetId = estimate.AssetId.HasValue && assetIdMap.TryGetValue(estimate.AssetId.Value, out var newAssetId) 
                    ? newAssetId : null;
                estimate.Lines.Clear(); // Will add separately
                _context.Estimates.Add(estimate);
                await _context.SaveChangesAsync();
                estimateIdMap[oldId] = estimate.Id;
                result.EstimatesImported++;
            }

            await _context.SaveChangesAsync();

            // Import estimate lines
            foreach (var line in backup.EstimateLines)
            {
                if (!estimateIdMap.TryGetValue(line.EstimateId, out var newEstimateId))
                    continue;

                line.Id = 0;
                line.EstimateId = newEstimateId;
                line.InventoryItemId = line.InventoryItemId.HasValue && inventoryIdMap.TryGetValue(line.InventoryItemId.Value, out var newInvId)
                    ? newInvId : null;
                _context.EstimateLines.Add(line);
            }

            await _context.SaveChangesAsync();

            // Import jobs
            foreach (var job in backup.Jobs)
            {
                var oldId = job.Id;
                if (!customerIdMap.TryGetValue(job.CustomerId, out var newCustomerId))
                    continue;

                job.Id = 0;
                job.CustomerId = newCustomerId;
                job.SiteId = job.SiteId.HasValue && siteIdMap.TryGetValue(job.SiteId.Value, out var newSiteId) 
                    ? newSiteId : null;
                job.AssetId = job.AssetId.HasValue && assetIdMap.TryGetValue(job.AssetId.Value, out var newAssetId) 
                    ? newAssetId : null;
                job.EstimateId = job.EstimateId.HasValue && estimateIdMap.TryGetValue(job.EstimateId.Value, out var newEstimateId) 
                    ? newEstimateId : null;
                job.TimeEntries.Clear(); // Will add separately
                _context.Jobs.Add(job);
                await _context.SaveChangesAsync();
                jobIdMap[oldId] = job.Id;
                result.JobsImported++;
            }

            await _context.SaveChangesAsync();

            // Import time entries
            foreach (var entry in backup.TimeEntries)
            {
                if (!jobIdMap.TryGetValue(entry.JobId, out var newJobId))
                    continue;

                entry.Id = 0;
                entry.JobId = newJobId;
                _context.TimeEntries.Add(entry);
            }

            await _context.SaveChangesAsync();

            // Import invoices
            foreach (var invoice in backup.Invoices)
            {
                var oldId = invoice.Id;
                if (!customerIdMap.TryGetValue(invoice.CustomerId, out var newCustomerId))
                    continue;

                // Check for existing invoice by number
                var existing = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.InvoiceNumber == invoice.InvoiceNumber);
                
                if (existing != null)
                {
                    invoiceIdMap[oldId] = existing.Id;
                    result.Warnings.Add($"Skipped duplicate invoice: {invoice.InvoiceNumber}");
                    continue;
                }

                invoice.Id = 0;
                invoice.CustomerId = newCustomerId;
                invoice.JobId = invoice.JobId.HasValue && jobIdMap.TryGetValue(invoice.JobId.Value, out var newJobId) 
                    ? newJobId : null;
                invoice.EstimateId = invoice.EstimateId.HasValue && estimateIdMap.TryGetValue(invoice.EstimateId.Value, out var newEstimateId) 
                    ? newEstimateId : null;
                invoice.Payments.Clear(); // Will add separately
                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();
                invoiceIdMap[oldId] = invoice.Id;
                result.InvoicesImported++;
            }

            await _context.SaveChangesAsync();

            // Import payments
            foreach (var payment in backup.Payments)
            {
                if (!invoiceIdMap.TryGetValue(payment.InvoiceId, out var newInvoiceId))
                    continue;

                payment.Id = 0;
                payment.InvoiceId = newInvoiceId;
                _context.Payments.Add(payment);
                result.PaymentsImported++;
            }

            await _context.SaveChangesAsync();

            // Import schema definitions
            foreach (var schema in backup.SchemaDefinitions)
            {
                var existing = await _context.SchemaDefinitions
                    .FirstOrDefaultAsync(s => s.EntityType == schema.EntityType && s.FieldName == schema.FieldName);

                if (existing == null)
                {
                    schema.Id = 0;
                    _context.SchemaDefinitions.Add(schema);
                }
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Import failed: {ex.Message}");
        }

        return result;
    }
}

/// <summary>
/// Container for backup data serialization.
/// </summary>
public class BackupData
{
    public DateTime ExportedAt { get; set; }
    public string Version { get; set; } = "2.0";
    
    // Phase 1 entities
    public List<Customer> Customers { get; set; } = [];
    public List<Site> Sites { get; set; } = [];
    public List<Asset> Assets { get; set; } = [];
    public List<CustomField> CustomFields { get; set; } = [];
    public List<SchemaDefinition> SchemaDefinitions { get; set; } = [];
    
    // Phase 2 entities
    public List<Estimate> Estimates { get; set; } = [];
    public List<EstimateLine> EstimateLines { get; set; } = [];
    public List<InventoryItem> InventoryItems { get; set; } = [];
    public List<InventoryLog> InventoryLogs { get; set; } = [];
    
    // Phase 3 entities
    public List<Job> Jobs { get; set; } = [];
    public List<TimeEntry> TimeEntries { get; set; } = [];
    public List<Invoice> Invoices { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
}
