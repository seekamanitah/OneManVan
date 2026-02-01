using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Shared.Services;

/// <summary>
/// Shared base implementation for backup/restore operations across Desktop, Mobile, and Web.
/// Platform-specific implementations inherit from this and provide platform-specific paths.
/// </summary>
public abstract class BackupServiceBase : IBackupService
{
    protected readonly OneManVanDbContext _context;
    protected readonly ISettingsStorage _settings;
    protected readonly string _backupDirectory;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected BackupServiceBase(OneManVanDbContext context, ISettingsStorage settings)
    {
        _context = context;
        _settings = settings;
        
        // Get backup directory from settings storage (platform-specific)
        var baseDir = settings.AppDataDirectory;
        _backupDirectory = Path.Combine(baseDir, "Backups");
        Directory.CreateDirectory(_backupDirectory);
    }

    public string GetBackupDirectory() => _backupDirectory;

    /// <summary>
    /// Creates a full backup of all data as JSON.
    /// </summary>
    public virtual async Task<BackupResult> CreateBackupAsync(string? customPath = null)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"OneManVan_Backup_{timestamp}.json";
            var filePath = customPath ?? Path.Combine(_backupDirectory, fileName);

            // Gather all data with AsNoTracking for performance
            var backupData = new BackupData
            {
                BackupDate = DateTime.UtcNow,
                AppVersion = "3.0",
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

            var json = JsonSerializer.Serialize(backupData, JsonOptions);
            await File.WriteAllTextAsync(filePath, json);

            return BackupResult.Succeeded(filePath, backupData.TotalRecordCount, 
                $"Backup created with {backupData.TotalRecordCount} records");
        }
        catch (Exception ex)
        {
            return BackupResult.Failed($"Backup failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a ZIP-compressed backup.
    /// </summary>
    public virtual async Task<BackupResult> CreateZipBackupAsync(string? customPath = null)
    {
        var tempJsonPath = Path.Combine(Path.GetTempPath(), $"OneManVan_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        
        try
        {
            // Create JSON backup first
            var jsonResult = await CreateBackupAsync(tempJsonPath);
            if (!jsonResult.Success)
            {
                return jsonResult;
            }

            // Create ZIP file
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var zipFileName = $"OneManVan_Backup_{timestamp}.zip";
            var zipPath = customPath ?? Path.Combine(_backupDirectory, zipFileName);

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            using (var zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                zipArchive.CreateEntryFromFile(tempJsonPath, "backup.json", CompressionLevel.Optimal);
                
                // Add metadata file
                var metadataEntry = zipArchive.CreateEntry("metadata.txt");
                using (var writer = new StreamWriter(metadataEntry.Open()))
                {
                    await writer.WriteLineAsync($"OneManVan Backup");
                    await writer.WriteLineAsync($"Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    await writer.WriteLineAsync($"Version: 3.0");
                    await writer.WriteLineAsync($"Records: {jsonResult.RecordCount}");
                }
            }

            return BackupResult.Succeeded(zipPath, jsonResult.RecordCount, 
                $"ZIP backup created with {jsonResult.RecordCount} records");
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempJsonPath))
            {
                try { File.Delete(tempJsonPath); } catch { }
            }
        }
    }

    /// <summary>
    /// Restores data from a backup file using merge mode (upsert).
    /// </summary>
    public virtual async Task<BackupResult> RestoreBackupAsync(string backupPath)
    {
        return await RestoreBackupAsync(backupPath, mergeMode: true);
    }

    /// <summary>
    /// Restores data from a backup file.
    /// </summary>
    /// <param name="backupPath">Path to the backup file (JSON or ZIP).</param>
    /// <param name="mergeMode">If true, uses merge/upsert. If false, replaces all data.</param>
    public virtual async Task<BackupResult> RestoreBackupAsync(string backupPath, bool mergeMode)
    {
        try
        {
            if (!File.Exists(backupPath))
            {
                return BackupResult.Failed("Backup file not found");
            }

            // Check if ZIP file
            if (backupPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return await RestoreFromZipAsync(backupPath, mergeMode);
            }

            // Restore from JSON
            return await RestoreFromJsonAsync(backupPath, mergeMode);
        }
        catch (Exception ex)
        {
            return BackupResult.Failed($"Restore failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Restores from a ZIP backup file.
    /// </summary>
    protected virtual async Task<BackupResult> RestoreFromZipAsync(string zipPath, bool mergeMode)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"OneManVan_Import_{Guid.NewGuid():N}");
        
        try
        {
            // Extract ZIP to temp directory
            Directory.CreateDirectory(tempDir);
            ZipFile.ExtractToDirectory(zipPath, tempDir);

            // Find the JSON backup file
            var jsonFile = Path.Combine(tempDir, "backup.json");
            if (!File.Exists(jsonFile))
            {
                // Try to find any JSON file
                var jsonFiles = Directory.GetFiles(tempDir, "*.json");
                if (jsonFiles.Length == 0)
                {
                    return BackupResult.Failed("No JSON backup file found in ZIP archive");
                }
                jsonFile = jsonFiles[0];
            }

            // Import from JSON
            return await RestoreFromJsonAsync(jsonFile, mergeMode);
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

    /// <summary>
    /// Restores from a JSON backup file.
    /// </summary>
    protected virtual async Task<BackupResult> RestoreFromJsonAsync(string jsonPath, bool mergeMode)
    {
        var json = await File.ReadAllTextAsync(jsonPath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var backupData = JsonSerializer.Deserialize<BackupData>(json, options);
        if (backupData == null)
        {
            return BackupResult.Failed("Invalid backup file format");
        }

        _context.ChangeTracker.Clear();

        if (mergeMode)
        {
            return await MergeRestoreAsync(backupData);
        }
        else
        {
            return await ReplaceRestoreAsync(backupData);
        }
    }

    /// <summary>
    /// Merge/upsert restore - updates existing records, inserts new ones.
    /// </summary>
    protected virtual async Task<BackupResult> MergeRestoreAsync(BackupData backupData)
    {
        var recordCount = 0;
        var customerIdMap = new Dictionary<int, int>(); // old ID -> new ID
        var siteIdMap = new Dictionary<int, int>();
        var assetIdMap = new Dictionary<int, int>();
        var estimateIdMap = new Dictionary<int, int>();
        var jobIdMap = new Dictionary<int, int>();
        var invoiceIdMap = new Dictionary<int, int>();

        // Process Customers
        foreach (var customer in backupData.Customers)
        {
            var oldId = customer.Id;
            var existing = await _context.Customers.FirstOrDefaultAsync(c => 
                c.Name == customer.Name && c.Email == customer.Email);

            if (existing != null)
            {
                // Update existing
                existing.Phone = customer.Phone;
                existing.SecondaryPhone = customer.SecondaryPhone;
                existing.Notes = customer.Notes;
                existing.Status = customer.Status;
                customerIdMap[oldId] = existing.Id;
            }
            else
            {
                // Insert new
                customer.Id = 0;
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
                customerIdMap[oldId] = customer.Id;
            }
            recordCount++;
        }
        await _context.SaveChangesAsync();

        // Process Sites
        foreach (var site in backupData.Sites)
        {
            var oldId = site.Id;
            if (!customerIdMap.TryGetValue(site.CustomerId, out var newCustomerId))
                continue;

            var existing = await _context.Sites.FirstOrDefaultAsync(s => 
                s.CustomerId == newCustomerId && s.Address == site.Address);

            if (existing == null)
            {
                site.Id = 0;
                site.CustomerId = newCustomerId;
                _context.Sites.Add(site);
                await _context.SaveChangesAsync();
                siteIdMap[oldId] = site.Id;
            }
            else
            {
                siteIdMap[oldId] = existing.Id;
            }
            recordCount++;
        }
        await _context.SaveChangesAsync();

        // Process Assets
        foreach (var asset in backupData.Assets)
        {
            var oldId = asset.Id;
            var existing = await _context.Assets.FirstOrDefaultAsync(a => a.Serial == asset.Serial);

            if (existing != null)
            {
                // Update existing
                existing.Brand = asset.Brand;
                existing.Model = asset.Model;
                existing.Nickname = asset.Nickname;
                existing.EquipmentType = asset.EquipmentType;
                assetIdMap[oldId] = existing.Id;
            }
            else if (asset.CustomerId.HasValue && customerIdMap.TryGetValue(asset.CustomerId.Value, out var newCustomerId))
            {
                asset.Id = 0;
                asset.CustomerId = newCustomerId;
                asset.SiteId = asset.SiteId.HasValue && siteIdMap.TryGetValue(asset.SiteId.Value, out var newSiteId) 
                    ? newSiteId : null;
                _context.Assets.Add(asset);
                await _context.SaveChangesAsync();
                assetIdMap[oldId] = asset.Id;
            }
            recordCount++;
        }
        await _context.SaveChangesAsync();

        // Process remaining entities (Estimates, Jobs, Invoices, etc.)
        recordCount += await ProcessEstimates(backupData, customerIdMap, assetIdMap, estimateIdMap);
        recordCount += await ProcessJobs(backupData, customerIdMap, assetIdMap, jobIdMap);
        recordCount += await ProcessInvoices(backupData, customerIdMap, jobIdMap, estimateIdMap, invoiceIdMap);
        recordCount += await ProcessInventory(backupData);

        return BackupResult.Succeeded(filePath: null!, recordCount, 
            $"Restore complete. Processed {recordCount} records.");
    }

    /// <summary>
    /// Full replace restore - clears existing data and inserts all backup data.
    /// </summary>
    protected virtual async Task<BackupResult> ReplaceRestoreAsync(BackupData backupData)
    {
        // Clear all tables in reverse dependency order
        _context.Payments.RemoveRange(_context.Payments);
        _context.Invoices.RemoveRange(_context.Invoices);
        _context.TimeEntries.RemoveRange(_context.TimeEntries);
        _context.Jobs.RemoveRange(_context.Jobs);
        _context.InventoryLogs.RemoveRange(_context.InventoryLogs);
        _context.InventoryItems.RemoveRange(_context.InventoryItems);
        _context.EstimateLines.RemoveRange(_context.EstimateLines);
        _context.Estimates.RemoveRange(_context.Estimates);
        _context.CustomFields.RemoveRange(_context.CustomFields);
        _context.Assets.RemoveRange(_context.Assets);
        _context.Sites.RemoveRange(_context.Sites);
        _context.Customers.RemoveRange(_context.Customers);
        _context.SchemaDefinitions.RemoveRange(_context.SchemaDefinitions);
        
        await _context.SaveChangesAsync();

        // Insert all backup data
        _context.Customers.AddRange(backupData.Customers);
        _context.Sites.AddRange(backupData.Sites);
        _context.Assets.AddRange(backupData.Assets);
        _context.CustomFields.AddRange(backupData.CustomFields);
        _context.SchemaDefinitions.AddRange(backupData.SchemaDefinitions);
        _context.Estimates.AddRange(backupData.Estimates);
        _context.EstimateLines.AddRange(backupData.EstimateLines);
        _context.InventoryItems.AddRange(backupData.InventoryItems);
        _context.InventoryLogs.AddRange(backupData.InventoryLogs);
        _context.Jobs.AddRange(backupData.Jobs);
        _context.TimeEntries.AddRange(backupData.TimeEntries);
        _context.Invoices.AddRange(backupData.Invoices);
        _context.Payments.AddRange(backupData.Payments);
        
        await _context.SaveChangesAsync();

        return BackupResult.Succeeded(filePath: null!, backupData.TotalRecordCount, 
            $"Replace complete. Restored {backupData.TotalRecordCount} records.");
    }

    #region Entity Processing Helpers

    protected virtual async Task<int> ProcessEstimates(BackupData data, Dictionary<int, int> customerIdMap, 
        Dictionary<int, int> assetIdMap, Dictionary<int, int> estimateIdMap)
    {
        var count = 0;
        foreach (var estimate in data.Estimates)
        {
            var oldId = estimate.Id;
            if (!customerIdMap.TryGetValue(estimate.CustomerId, out var newCustomerId))
                continue;

            estimate.Id = 0;
            estimate.CustomerId = newCustomerId;
            estimate.AssetId = estimate.AssetId.HasValue && assetIdMap.TryGetValue(estimate.AssetId.Value, out var newAssetId) 
                ? newAssetId : null;
            _context.Estimates.Add(estimate);
            await _context.SaveChangesAsync();
            estimateIdMap[oldId] = estimate.Id;
            count++;
        }

        // Process estimate lines
        foreach (var line in data.EstimateLines)
        {
            if (estimateIdMap.TryGetValue(line.EstimateId, out var newEstimateId))
            {
                line.Id = 0;
                line.EstimateId = newEstimateId;
                _context.EstimateLines.Add(line);
                count++;
            }
        }
        await _context.SaveChangesAsync();
        return count;
    }

    protected virtual async Task<int> ProcessJobs(BackupData data, Dictionary<int, int> customerIdMap, 
        Dictionary<int, int> assetIdMap, Dictionary<int, int> jobIdMap)
    {
        var count = 0;
        foreach (var job in data.Jobs)
        {
            var oldId = job.Id;
            if (!customerIdMap.TryGetValue(job.CustomerId, out var newCustomerId))
                continue;

            job.Id = 0;
            job.CustomerId = newCustomerId;
            job.AssetId = job.AssetId.HasValue && assetIdMap.TryGetValue(job.AssetId.Value, out var newAssetId) 
                ? newAssetId : null;
            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();
            jobIdMap[oldId] = job.Id;
            count++;
        }

        // Process time entries
        foreach (var timeEntry in data.TimeEntries)
        {
            if (jobIdMap.TryGetValue(timeEntry.JobId, out var newJobId))
            {
                timeEntry.Id = 0;
                timeEntry.JobId = newJobId;
                _context.TimeEntries.Add(timeEntry);
                count++;
            }
        }
        await _context.SaveChangesAsync();
        return count;
    }

    protected virtual async Task<int> ProcessInvoices(BackupData data, Dictionary<int, int> customerIdMap, 
        Dictionary<int, int> jobIdMap, Dictionary<int, int> estimateIdMap, Dictionary<int, int> invoiceIdMap)
    {
        var count = 0;
        foreach (var invoice in data.Invoices)
        {
            var oldId = invoice.Id;
            if (!customerIdMap.TryGetValue(invoice.CustomerId, out var newCustomerId))
                continue;

            invoice.Id = 0;
            invoice.CustomerId = newCustomerId;
            invoice.JobId = invoice.JobId.HasValue && jobIdMap.TryGetValue(invoice.JobId.Value, out var newJobId) 
                ? newJobId : null;
            invoice.EstimateId = invoice.EstimateId.HasValue && estimateIdMap.TryGetValue(invoice.EstimateId.Value, out var newEstimateId) 
                ? newEstimateId : null;
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
            invoiceIdMap[oldId] = invoice.Id;
            count++;
        }

        // Process payments
        foreach (var payment in data.Payments)
        {
            if (invoiceIdMap.TryGetValue(payment.InvoiceId, out var newInvoiceId))
            {
                payment.Id = 0;
                payment.InvoiceId = newInvoiceId;
                _context.Payments.Add(payment);
                count++;
            }
        }
        await _context.SaveChangesAsync();
        return count;
    }

    protected virtual async Task<int> ProcessInventory(BackupData data)
    {
        var count = 0;
        var itemIdMap = new Dictionary<int, int>();

        foreach (var item in data.InventoryItems)
        {
            var oldId = item.Id;
            var existing = await _context.InventoryItems.FirstOrDefaultAsync(i => i.Sku == item.Sku);

            if (existing != null)
            {
                existing.Name = item.Name;
                existing.Description = item.Description;
                existing.QuantityOnHand = item.QuantityOnHand;
                existing.Cost = item.Cost;
                existing.Price = item.Price;
                itemIdMap[oldId] = existing.Id;
            }
            else
            {
                item.Id = 0;
                _context.InventoryItems.Add(item);
                await _context.SaveChangesAsync();
                itemIdMap[oldId] = item.Id;
            }
            count++;
        }
        await _context.SaveChangesAsync();

        // Process inventory logs
        foreach (var log in data.InventoryLogs)
        {
            if (itemIdMap.TryGetValue(log.InventoryItemId, out var newItemId))
            {
                log.Id = 0;
                log.InventoryItemId = newItemId;
                _context.InventoryLogs.Add(log);
                count++;
            }
        }
        await _context.SaveChangesAsync();
        return count;
    }

    #endregion

    #region IBackupService Implementation

    public virtual async Task<IEnumerable<BackupInfo>> GetAvailableBackupsAsync()
    {
        if (!Directory.Exists(_backupDirectory))
        {
            return Enumerable.Empty<BackupInfo>();
        }

        var files = Directory.GetFiles(_backupDirectory, "*.json")
            .Concat(Directory.GetFiles(_backupDirectory, "*.zip"));

        var backups = files.Select(f =>
        {
            var fileInfo = new FileInfo(f);
            return new BackupInfo
            {
                FilePath = f,
                FileName = fileInfo.Name,
                CreatedAt = fileInfo.CreationTime,
                FileSizeBytes = fileInfo.Length
            };
        }).OrderByDescending(b => b.CreatedAt);

        return await Task.FromResult(backups);
    }

    public virtual async Task<bool> DeleteBackupAsync(string backupPath)
    {
        try
        {
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
                return await Task.FromResult(true);
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public virtual async Task<string> ExportEntityAsync<T>(T entity) where T : class
    {
        var json = JsonSerializer.Serialize(entity, JsonOptions);
        var fileName = $"{typeof(T).Name}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        var filePath = Path.Combine(_backupDirectory, fileName);
        await File.WriteAllTextAsync(filePath, json);
        return filePath;
    }

    #endregion
}
