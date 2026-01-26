using System.IO;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Services;

/// <summary>
/// Service for exporting and importing data as CSV files (Desktop version).
/// CSV files can be edited in Excel and re-imported.
/// </summary>
public class CsvExportImportService
{
    private readonly OneManVanDbContext _db;
    private readonly string _exportDirectory;

    public CsvExportImportService(OneManVanDbContext db)
    {
        _db = db;
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        _exportDirectory = Path.Combine(documentsPath, "OneManVan", "CSV Exports");
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
            
            // Header
            csv.AppendLine("Id,Name,CompanyName,Email,Phone,CustomerType,Status,Notes,CreatedAt");
            
            // Data rows
            foreach (var c in customers)
            {
                csv.AppendLine($"{c.Id},{Escape(c.Name)},{Escape(c.CompanyName)},{Escape(c.Email)},{Escape(c.Phone)},{c.CustomerType},{c.Status},{Escape(c.Notes)},{c.CreatedAt:yyyy-MM-dd}");
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
            csv.AppendLine("Id,Sku,Name,Description,Category,QuantityOnHand,ReorderPoint,Cost,Price,Unit,Location,Supplier,PartNumber");
            
            // Data rows
            foreach (var i in items)
            {
                csv.AppendLine($"{i.Id},{Escape(i.Sku)},{Escape(i.Name)},{Escape(i.Description)},{i.Category},{i.QuantityOnHand},{i.ReorderPoint},{i.Cost:F2},{i.Price:F2},{Escape(i.Unit)},{Escape(i.Location)},{Escape(i.Supplier)},{Escape(i.PartNumber)}");
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
            var assets = await _db.Assets.Include(a => a.Customer).OrderBy(a => a.Customer!.Name).ToListAsync();
            var csv = new StringBuilder();
            
            // Header
            csv.AppendLine("Id,AssetTag,Serial,Brand,Model,Nickname,CustomerName,EquipmentType,InstallDate,Notes");
            
            // Data rows
            foreach (var a in assets)
            {
                csv.AppendLine($"{a.Id},{Escape(a.AssetTag)},{Escape(a.Serial)},{Escape(a.Brand)},{Escape(a.Model)},{Escape(a.Nickname)},{Escape(a.Customer?.Name)},{a.EquipmentType},{a.InstallDate:yyyy-MM-dd},{Escape(a.Notes)}");
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
                    if (fields.Length < 8) continue; // Need at least 8 fields

                    var id = int.TryParse(fields[0], out var parsedId) ? parsedId : 0;
                    var name = fields[1];
                    var companyName = fields[2];
                    var email = fields[3];
                    var phone = fields[4];
                    var customerType = Enum.TryParse<Shared.Models.Enums.CustomerType>(fields[5], out var type) 
                        ? type : Shared.Models.Enums.CustomerType.Residential;
                    var status = Enum.TryParse<Shared.Models.Enums.CustomerStatus>(fields[6], out var stat) 
                        ? stat : Shared.Models.Enums.CustomerStatus.Active;
                    var notes = fields.Length > 7 ? fields[7] : "";

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        result.SkippedRows++;
                        continue;
                    }

                    Customer? customer = null;
                    
                    if (mergeMode && id > 0)
                    {
                        // Try to find existing customer by ID
                        customer = await _db.Customers.FindAsync(id);
                    }

                    if (customer == null)
                    {
                        // Create new customer
                        customer = new Customer
                        {
                            Name = name,
                            CompanyName = companyName,
                            Email = email,
                            Phone = phone,
                            CustomerType = customerType,
                            Status = status,
                            Notes = notes,
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.Customers.Add(customer);
                        result.AddedRecords++;
                    }
                    else
                    {
                        // Update existing customer
                        customer.Name = name;
                        customer.CompanyName = companyName;
                        customer.Email = email;
                        customer.Phone = phone;
                        customer.CustomerType = customerType;
                        customer.Status = status;
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
                    var category = Enum.TryParse<Shared.Models.Enums.InventoryCategory>(fields[4], out var cat) 
                        ? cat : Shared.Models.Enums.InventoryCategory.General;
                    var quantityOnHand = int.TryParse(fields[5], out var qty) ? qty : 0;
                    var reorderPoint = int.TryParse(fields[6], out var reorder) ? reorder : 0;
                    var cost = decimal.TryParse(fields[7], out var cst) ? cst : 0;
                    var price = decimal.TryParse(fields[8], out var prc) ? prc : 0;
                    var unit = fields.Length > 9 ? fields[9] : "";
                    var location = fields.Length > 10 ? fields[10] : "";
                    var supplier = fields.Length > 11 ? fields[11] : "";
                    var partNumber = fields.Length > 12 ? fields[12] : "";

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
                            IsActive = true
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
