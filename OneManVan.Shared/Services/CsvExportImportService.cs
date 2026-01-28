using System.Text;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Services;

/// <summary>
/// Shared service for exporting and importing data as CSV files.
/// Works across Desktop, Mobile, and Web platforms.
/// CSV files can be edited in Excel and re-imported.
/// </summary>
public class CsvExportImportService
{
    private readonly OneManVanDbContext _db;
    private readonly ISettingsStorage _settings;
    private readonly string _exportDirectory;

    public CsvExportImportService(OneManVanDbContext db, ISettingsStorage settings)
    {
        _db = db;
        _settings = settings;
        
        // Get export directory from settings storage (platform-specific)
        var baseDir = _settings.AppDataDirectory;
        _exportDirectory = Path.Combine(baseDir, "CSV Exports");
        Directory.CreateDirectory(_exportDirectory);
    }

    public string GetExportDirectory() => _exportDirectory;

    #region Export Methods

    /// <summary>
    /// Exports all customers to CSV.
    /// </summary>
    public async Task<CsvExportResult> ExportCustomersAsync()
    {
        try
        {
            var customers = await _db.Customers.OrderBy(c => c.Name).ToListAsync();
            var csv = new StringBuilder();
            
            // Header - Using ACTUAL current Customer properties
            csv.AppendLine("Id,Name,Email,Phone,SecondaryPhone,Status,HomeAddress,Notes,CreatedAt");
            
            // Data rows
            foreach (var c in customers)
            {
                csv.AppendLine($"{c.Id},{Escape(c.Name)},{Escape(c.Email)},{Escape(c.Phone)},{Escape(c.SecondaryPhone)},{c.Status}," +
                    $"{Escape(c.HomeAddress)},{Escape(c.Notes)},{c.CreatedAt:yyyy-MM-dd}");
            }

            var fileName = $"Customers_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(_exportDirectory, fileName);
            await File.WriteAllTextAsync(filePath, csv.ToString());

            return CsvExportResult.Succeeded(filePath, customers.Count, "Customers");
        }
        catch (Exception ex)
        {
            return CsvExportResult.Failed($"Export failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Exports all inventory items to CSV.
    /// </summary>
    public async Task<CsvExportResult> ExportInventoryAsync()
    {
        try
        {
            var items = await _db.InventoryItems.Where(i => i.IsActive).OrderBy(i => i.Name).ToListAsync();
            var csv = new StringBuilder();
            
            // Header
            csv.AppendLine("Id,Sku,Name,Description,Category,QuantityOnHand,ReorderPoint,Cost,Price,Unit,Location,Supplier,PartNumber,IsActive");
            
            // Data rows
            foreach (var i in items)
            {
                csv.AppendLine($"{i.Id},{Escape(i.Sku)},{Escape(i.Name)},{Escape(i.Description)},{i.Category}," +
                    $"{i.QuantityOnHand},{i.ReorderPoint},{i.Cost:F2},{i.Price:F2},{Escape(i.Unit)}," +
                    $"{Escape(i.Location)},{Escape(i.Supplier)},{Escape(i.PartNumber)},{i.IsActive}");
            }

            var fileName = $"Inventory_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(_exportDirectory, fileName);
            await File.WriteAllTextAsync(filePath, csv.ToString());

            return CsvExportResult.Succeeded(filePath, items.Count, "Inventory");
        }
        catch (Exception ex)
        {
            return CsvExportResult.Failed($"Export failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Exports all assets to CSV.
    /// </summary>
    public async Task<CsvExportResult> ExportAssetsAsync()
    {
        try
        {
            var assets = await _db.Assets
                .Include(a => a.Customer)
                .OrderBy(a => a.Customer!.Name)
                .AsNoTracking()
                .ToListAsync();
            
            var csv = new StringBuilder();
            
            // Header - Using ACTUAL Asset property names
            csv.AppendLine("Id,AssetTag,Serial,Brand,Model,Nickname,CustomerId,CustomerName,EquipmentType,InstallDate,Notes");
            
            // Data rows
            foreach (var a in assets)
            {
                csv.AppendLine($"{a.Id},{Escape(a.AssetTag)},{Escape(a.Serial)},{Escape(a.Brand)}," +
                    $"{Escape(a.Model)},{Escape(a.Nickname)},{a.CustomerId},{Escape(a.Customer?.Name)}," +
                    $"{a.EquipmentType},{a.InstallDate:yyyy-MM-dd},{Escape(a.Notes)}");
            }

            var fileName = $"Assets_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(_exportDirectory, fileName);
            await File.WriteAllTextAsync(filePath, csv.ToString());

            return CsvExportResult.Succeeded(filePath, assets.Count, "Assets");
        }
        catch (Exception ex)
        {
            return CsvExportResult.Failed($"Export failed: {ex.Message}");
        }
    }

    #endregion

    #region Import Methods

    /// <summary>
    /// Imports customers from CSV file.
    /// </summary>
    public async Task<CsvImportResult> ImportCustomersAsync(string filePath, bool mergeMode = true)
    {
        var result = new CsvImportResult();
        
        try
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            if (lines.Length < 2)
            {
                result.Success = false;
                result.Message = "CSV file is empty or contains only headers";
                return result;
            }

            // Skip header row
            for (int i = 1; i < lines.Length; i++)
            {
                try
                {
                    var fields = ParseCsvLine(lines[i]);
                    if (fields.Length < 6) continue; // Need at least ID, Name, Email, Phone, Status minimum

                    var id = int.TryParse(fields[0], out var parsedId) ? parsedId : 0;
                    var name = fields[1];
                    var email = fields[2];
                    var phone = fields[3];
                    var secondaryPhone = fields.Length > 4 ? fields[4] : "";
                    var status = fields.Length > 5 && Enum.TryParse<CustomerStatus>(fields[5], out var stat) 
                        ? stat : CustomerStatus.Active;
                    
                    // Home address (single field)
                    var homeAddress = fields.Length > 6 ? fields[6] : "";
                    var notes = fields.Length > 7 ? fields[7] : "";

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        result.SkippedRows++;
                        continue;
                    }

                    Customer? customer = null;
                    
                    if (mergeMode && id > 0)
                    {
                        customer = await _db.Customers.FindAsync(id);
                    }

                    if (customer == null)
                    {
                        customer = new Customer
                        {
                            Name = name,
                            Email = email,
                            Phone = phone,
                            SecondaryPhone = secondaryPhone,
                            Status = status,
                            HomeAddress = homeAddress,
                            Notes = notes,
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.Customers.Add(customer);
                        result.AddedRecords++;
                    }
                    else
                    {
                        customer.Name = name;
                        customer.Email = email;
                        customer.Phone = phone;
                        customer.SecondaryPhone = secondaryPhone;
                        customer.Status = status;
                        customer.HomeAddress = homeAddress;
                        customer.Notes = notes;
                        result.UpdatedRecords++;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Row {i + 1}: {ex.Message}");
                    result.SkippedRows++;
                }
            }

            await _db.SaveChangesAsync();
            result.Success = true;
            result.Message = $"Import complete. Added: {result.AddedRecords}, Updated: {result.UpdatedRecords}, Skipped: {result.SkippedRows}";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Import failed: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Imports inventory items from CSV file.
    /// </summary>
    public async Task<CsvImportResult> ImportInventoryAsync(string filePath, bool mergeMode = true)
    {
        var result = new CsvImportResult();
        
        try
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            if (lines.Length < 2)
            {
                result.Success = false;
                result.Message = "CSV file is empty or contains only headers";
                return result;
            }

            for (int i = 1; i < lines.Length; i++)
            {
                try
                {
                    var fields = ParseCsvLine(lines[i]);
                    if (fields.Length < 9) continue;

                    var id = int.TryParse(fields[0], out var parsedId) ? parsedId : 0;
                    var sku = fields[1];
                    var name = fields[2];
                    var description = fields[3];
                    var category = Enum.TryParse<InventoryCategory>(fields[4], out var cat) 
                        ? cat : InventoryCategory.General;
                    var quantityOnHand = int.TryParse(fields[5], out var qty) ? qty : 0;
                    var reorderPoint = int.TryParse(fields[6], out var reorder) ? reorder : 0;
                    var cost = decimal.TryParse(fields[7], out var cst) ? cst : 0;
                    var price = decimal.TryParse(fields[8], out var prc) ? prc : 0;
                    var unit = fields.Length > 9 ? fields[9] : "";
                    var location = fields.Length > 10 ? fields[10] : "";
                    var supplier = fields.Length > 11 ? fields[11] : "";
                    var partNumber = fields.Length > 12 ? fields[12] : "";
                    var isActive = fields.Length > 13 ? bool.TryParse(fields[13], out var active) && active : true;

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        result.SkippedRows++;
                        continue;
                    }

                    InventoryItem? item = null;
                    
                    if (mergeMode && id > 0)
                    {
                        item = await _db.InventoryItems.FindAsync(id);
                    }

                    if (item == null)
                    {
                        item = new InventoryItem
                        {
                            Sku = sku,
                            Name = name,
                            Description = description,
                            Category = category,
                            QuantityOnHand = quantityOnHand,
                            ReorderPoint = reorderPoint,
                            Cost = cost,
                            Price = price,
                            Unit = unit,
                            Location = location,
                            Supplier = supplier,
                            PartNumber = partNumber,
                            IsActive = isActive
                        };
                        _db.InventoryItems.Add(item);
                        result.AddedRecords++;
                    }
                    else
                    {
                        item.Sku = sku;
                        item.Name = name;
                        item.Description = description;
                        item.Category = category;
                        item.QuantityOnHand = quantityOnHand;
                        item.ReorderPoint = reorderPoint;
                        item.Cost = cost;
                        item.Price = price;
                        item.Unit = unit;
                        item.Location = location;
                        item.Supplier = supplier;
                        item.PartNumber = partNumber;
                        item.IsActive = isActive;
                        result.UpdatedRecords++;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Row {i + 1}: {ex.Message}");
                    result.SkippedRows++;
                }
            }

            await _db.SaveChangesAsync();
            result.Success = true;
            result.Message = $"Import complete. Added: {result.AddedRecords}, Updated: {result.UpdatedRecords}, Skipped: {result.SkippedRows}";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Import failed: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Parses a CSV line handling quoted fields.
    /// </summary>
    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    currentField.Append('"');
                    i++; // Skip next quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }

        fields.Add(currentField.ToString());
        return fields.ToArray();
    }

    #endregion

    #region Helper Methods

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    #endregion
}

#region Result Classes

/// <summary>
/// Result of a CSV export operation.
/// </summary>
public class CsvExportResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? FilePath { get; set; }
    public int RecordCount { get; set; }
    public string? EntityType { get; set; }

    public static CsvExportResult Succeeded(string filePath, int recordCount, string entityType)
    {
        return new CsvExportResult
        {
            Success = true,
            FilePath = filePath,
            RecordCount = recordCount,
            EntityType = entityType,
            Message = $"Exported {recordCount} {entityType} records"
        };
    }

    public static CsvExportResult Failed(string message)
    {
        return new CsvExportResult
        {
            Success = false,
            Message = message
        };
    }
}

/// <summary>
/// Result of a CSV import operation.
/// </summary>
public class CsvImportResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int AddedRecords { get; set; }
    public int UpdatedRecords { get; set; }
    public int SkippedRows { get; set; }
    public List<string> Errors { get; set; } = new();
    
    public int TotalProcessed => AddedRecords + UpdatedRecords;
}

#endregion
