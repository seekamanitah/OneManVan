using System.Text;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Mobile.Services;

/// <summary>
/// Service for exporting and importing data as CSV files.
/// CSV files can be edited in Excel and re-imported.
/// </summary>
public class CsvExportImportService
{
    private readonly OneManVanDbContext _db;
    private readonly string _exportDirectory;

    public CsvExportImportService(OneManVanDbContext db)
    {
        _db = db;
        _exportDirectory = Path.Combine(FileSystem.AppDataDirectory, "Exports");
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
            csv.AppendLine("Id,Sku,Name,Description,Category,QuantityOnHand,ReorderPoint,Cost,Price,Unit,Location,Supplier,PartNumber,IsActive");
            
            // Data rows
            foreach (var i in items)
            {
                csv.AppendLine($"{i.Id},{Escape(i.Sku)},{Escape(i.Name)},{Escape(i.Description)},{i.Category},{i.QuantityOnHand},{i.ReorderPoint},{i.Cost:F2},{i.Price:F2},{Escape(i.Unit)},{Escape(i.Location)},{Escape(i.Supplier)},{Escape(i.PartNumber)},{i.IsActive}");
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
            csv.AppendLine("Id,CustomerName,Nickname,Brand,Model,Serial,EquipmentType,FuelType,RefrigerantType,TonnageX10,BtuRating,SeerRating,AfueRating,InstallDate,WarrantyTermYears,Status,Notes");
            
            // Data rows
            foreach (var a in assets)
            {
                csv.AppendLine($"{a.Id},{Escape(a.Customer?.Name)},{Escape(a.Nickname)},{Escape(a.Brand)},{Escape(a.Model)},{Escape(a.Serial)},{a.EquipmentType},{a.FuelType},{a.RefrigerantType},{a.TonnageX10},{a.BtuRating},{a.SeerRating},{a.AfueRating},{a.InstallDate:yyyy-MM-dd},{a.WarrantyTermYears},{a.Status},{Escape(a.TechnicalNotes)}");
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

    /// <summary>
    /// Exports all jobs to CSV.
    /// </summary>
    public async Task<CsvExportResult> ExportJobsAsync()
    {
        try
        {
            var jobs = await _db.Jobs.Include(j => j.Customer).OrderByDescending(j => j.ScheduledDate).ToListAsync();
            var csv = new StringBuilder();
            
            // Header
            csv.AppendLine("Id,CustomerName,Title,Description,Status,Priority,ScheduledDate,StartedAt,CompletedAt,LaborTotal,PartsTotal,TaxAmount,Total,WorkPerformed");
            
            // Data rows
            foreach (var j in jobs)
            {
                csv.AppendLine($"{j.Id},{Escape(j.Customer?.Name)},{Escape(j.Title)},{Escape(j.Description)},{j.Status},{j.Priority},{j.ScheduledDate:yyyy-MM-dd HH:mm},{j.StartedAt:yyyy-MM-dd HH:mm},{j.CompletedAt:yyyy-MM-dd HH:mm},{j.LaborTotal:F2},{j.PartsTotal:F2},{j.TaxAmount:F2},{j.Total:F2},{Escape(j.WorkPerformed)}");
            }

            var fileName = $"Jobs_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(_exportDirectory, fileName);
            await File.WriteAllTextAsync(filePath, csv.ToString());

            return CsvExportResult.Succeeded(filePath, jobs.Count, "Jobs");
        }
        catch (Exception ex)
        {
            return CsvExportResult.Failed($"Export failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Exports all invoices to CSV.
    /// </summary>
    public async Task<CsvExportResult> ExportInvoicesAsync()
    {
        try
        {
            var invoices = await _db.Invoices.Include(i => i.Customer).OrderByDescending(i => i.CreatedAt).ToListAsync();
            var csv = new StringBuilder();
            
            // Header
            csv.AppendLine("Id,InvoiceNumber,CustomerName,Status,LaborAmount,PartsAmount,SubTotal,TaxRate,TaxAmount,Total,AmountPaid,BalanceDue,DueDate,PaidAt,Notes");
            
            // Data rows
            foreach (var i in invoices)
            {
                csv.AppendLine($"{i.Id},{Escape(i.InvoiceNumber)},{Escape(i.Customer?.Name)},{i.Status},{i.LaborAmount:F2},{i.PartsAmount:F2},{i.SubTotal:F2},{i.TaxRate:F2},{i.TaxAmount:F2},{i.Total:F2},{i.AmountPaid:F2},{i.BalanceDue:F2},{i.DueDate:yyyy-MM-dd},{i.PaidAt:yyyy-MM-dd},{Escape(i.Notes)}");
            }

            var fileName = $"Invoices_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(_exportDirectory, fileName);
            await File.WriteAllTextAsync(filePath, csv.ToString());

            return CsvExportResult.Succeeded(filePath, invoices.Count, "Invoices");
        }
        catch (Exception ex)
        {
            return CsvExportResult.Failed($"Export failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Exports ALL data to a single CSV file with sections for each entity type.
    /// This creates a comprehensive export that can be edited in Excel and re-imported.
    /// </summary>
    public async Task<CsvExportResult> ExportAllDataAsync()
    {
        try
        {
            var csv = new StringBuilder();
            var totalRecords = 0;

            // ===== CUSTOMERS SECTION =====
            csv.AppendLine("### CUSTOMERS ###");
            csv.AppendLine("Id,Name,CompanyName,Email,Phone,CustomerType,Status,Notes,CreatedAt");
            var customers = await _db.Customers.OrderBy(c => c.Name).ToListAsync();
            foreach (var c in customers)
            {
                csv.AppendLine($"{c.Id},{Escape(c.Name)},{Escape(c.CompanyName)},{Escape(c.Email)},{Escape(c.Phone)},{c.CustomerType},{c.Status},{Escape(c.Notes)},{c.CreatedAt:yyyy-MM-dd}");
            }
            totalRecords += customers.Count;
            csv.AppendLine();

            // ===== SITES SECTION =====
            csv.AppendLine("### SITES ###");
            csv.AppendLine("Id,CustomerId,CustomerName,Address,City,State,ZipCode,PropertyType,Notes");
            var sites = await _db.Sites.Include(s => s.Customer).OrderBy(s => s.Address).ToListAsync();
            foreach (var s in sites)
            {
                csv.AppendLine($"{s.Id},{s.CustomerId},{Escape(s.Customer?.Name)},{Escape(s.Address)},{Escape(s.City)},{Escape(s.State)},{Escape(s.ZipCode)},{s.PropertyType},{Escape(s.Notes)}");
            }
            totalRecords += sites.Count;
            csv.AppendLine();

            // ===== ASSETS SECTION =====
            csv.AppendLine("### ASSETS ###");
            csv.AppendLine("Id,CustomerId,CustomerName,SiteId,SiteAddress,Serial,Brand,Model,Nickname,EquipmentType,FuelType,InstallDate,WarrantyEndDate,Condition,Notes");
            var assets = await _db.Assets.Include(a => a.Customer).Include(a => a.Site).OrderBy(a => a.Serial).ToListAsync();
            foreach (var a in assets)
            {
                csv.AppendLine($"{a.Id},{a.CustomerId},{Escape(a.Customer?.Name)},{a.SiteId},{Escape(a.Site?.Address)},{Escape(a.Serial)},{Escape(a.Brand)},{Escape(a.Model)},{Escape(a.Nickname)},{a.EquipmentType},{a.FuelType},{a.InstallDate:yyyy-MM-dd},{a.WarrantyEndDate:yyyy-MM-dd},{a.Condition},{Escape(a.Notes)}");
            }
            totalRecords += assets.Count;
            csv.AppendLine();

            // ===== INVENTORY SECTION =====
            csv.AppendLine("### INVENTORY ###");
            csv.AppendLine("Id,Sku,Name,Description,Category,QuantityOnHand,ReorderPoint,Cost,Price,Unit,Location,Supplier,PartNumber,IsActive");
            var inventory = await _db.InventoryItems.Where(i => i.IsActive).OrderBy(i => i.Name).ToListAsync();
            foreach (var i in inventory)
            {
                csv.AppendLine($"{i.Id},{Escape(i.Sku)},{Escape(i.Name)},{Escape(i.Description)},{i.Category},{i.QuantityOnHand},{i.ReorderPoint},{i.Cost:F2},{i.Price:F2},{Escape(i.Unit)},{Escape(i.Location)},{Escape(i.Supplier)},{Escape(i.PartNumber)},{i.IsActive}");
            }
            totalRecords += inventory.Count;
            csv.AppendLine();

            // ===== PRODUCTS SECTION =====
            csv.AppendLine("### PRODUCTS ###");
            csv.AppendLine("Id,ProductName,Manufacturer,ModelNumber,Description,Category,SuggestedSellPrice,WholesaleCost,IsActive");
            var products = await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Manufacturer).ToListAsync();
            foreach (var p in products)
            {
                csv.AppendLine($"{p.Id},{Escape(p.ProductName)},{Escape(p.Manufacturer)},{Escape(p.ModelNumber)},{Escape(p.Description)},{p.Category},{p.SuggestedSellPrice:F2},{p.WholesaleCost:F2},{p.IsActive}");
            }
            totalRecords += products.Count;
            csv.AppendLine();

            // ===== JOBS SECTION =====
            csv.AppendLine("### JOBS ###");
            csv.AppendLine("Id,CustomerName,SiteAddress,Title,Description,Status,Priority,ScheduledDate,ScheduledEndDate,CompletedAt,EstimatedHours,Notes");
            var jobs = await _db.Jobs.Include(j => j.Customer).Include(j => j.Site).OrderByDescending(j => j.ScheduledDate).ToListAsync();
            foreach (var j in jobs)
            {
                csv.AppendLine($"{j.Id},{Escape(j.Customer?.Name)},{Escape(j.Site?.Address)},{Escape(j.Title)},{Escape(j.Description)},{j.Status},{j.Priority},{j.ScheduledDate:yyyy-MM-dd HH:mm},{j.ScheduledEndDate:yyyy-MM-dd HH:mm},{j.CompletedAt:yyyy-MM-dd HH:mm},{j.EstimatedHours:F2},{Escape(j.Notes)}");
            }
            totalRecords += jobs.Count;
            csv.AppendLine();

            // ===== ESTIMATES SECTION =====
            csv.AppendLine("### ESTIMATES ###");
            csv.AppendLine("Id,Title,CustomerName,Status,SubTotal,TaxRate,TaxAmount,Total,ExpiresAt,Notes");
            var estimates = await _db.Estimates.Include(e => e.Customer).OrderByDescending(e => e.CreatedAt).ToListAsync();
            foreach (var e in estimates)
            {
                csv.AppendLine($"{e.Id},{Escape(e.Title)},{Escape(e.Customer?.Name)},{e.Status},{e.SubTotal:F2},{e.TaxRate:F2},{e.TaxAmount:F2},{e.Total:F2},{e.ExpiresAt:yyyy-MM-dd},{Escape(e.Notes)}");
            }
            totalRecords += estimates.Count;
            csv.AppendLine();

            // ===== INVOICES SECTION =====
            csv.AppendLine("### INVOICES ###");
            csv.AppendLine("Id,InvoiceNumber,CustomerName,Status,LaborAmount,PartsAmount,SubTotal,TaxRate,TaxAmount,Total,AmountPaid,BalanceDue,DueDate,PaidAt,Notes");
            var invoices = await _db.Invoices.Include(i => i.Customer).OrderByDescending(i => i.CreatedAt).ToListAsync();
            foreach (var i in invoices)
            {
                csv.AppendLine($"{i.Id},{Escape(i.InvoiceNumber)},{Escape(i.Customer?.Name)},{i.Status},{i.LaborAmount:F2},{i.PartsAmount:F2},{i.SubTotal:F2},{i.TaxRate:F2},{i.TaxAmount:F2},{i.Total:F2},{i.AmountPaid:F2},{i.BalanceDue:F2},{i.DueDate:yyyy-MM-dd},{i.PaidAt:yyyy-MM-dd},{Escape(i.Notes)}");
            }
            totalRecords += invoices.Count;
            csv.AppendLine();

            // ===== SERVICE AGREEMENTS SECTION =====
            csv.AppendLine("### SERVICE_AGREEMENTS ###");
            csv.AppendLine("Id,CustomerName,AgreementNumber,Name,Status,StartDate,EndDate,IncludedVisitsPerYear,AnnualPrice");
            var agreements = await _db.ServiceAgreements.Include(sa => sa.Customer).OrderByDescending(sa => sa.StartDate).ToListAsync();
            foreach (var sa in agreements)
            {
                csv.AppendLine($"{sa.Id},{Escape(sa.Customer?.Name)},{Escape(sa.AgreementNumber)},{Escape(sa.Name)},{sa.Status},{sa.StartDate:yyyy-MM-dd},{sa.EndDate:yyyy-MM-dd},{sa.IncludedVisitsPerYear},{sa.AnnualPrice:F2}");
            }
            totalRecords += agreements.Count;

            // Save file
            var fileName = $"AllData_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(_exportDirectory, fileName);
            await File.WriteAllTextAsync(filePath, csv.ToString());

            return CsvExportResult.Succeeded(filePath, totalRecords, "All Data");
        }
        catch (Exception ex)
        {
            return CsvExportResult.Failed($"Export failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Imports all data from a comprehensive CSV export file.
    /// Sections are identified by ### ENTITY_NAME ### markers.
    /// </summary>
    public async Task<CsvImportResult> ImportAllDataAsync(string filePath)
    {
        var result = new CsvImportResult { EntityType = "All Data" };

        try
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            if (lines.Length < 2)
            {
                result.Errors.Add("File is empty or has no data.");
                return result;
            }

            string currentSection = "";
            string[]? headers = null;
            int lineNum = 0;

            foreach (var line in lines)
            {
                lineNum++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Check for section marker
                if (line.StartsWith("### ") && line.EndsWith(" ###"))
                {
                    currentSection = line.Replace("###", "").Trim().ToUpperInvariant();
                    headers = null; // Reset headers for new section
                    continue;
                }

                // If we're in a section and don't have headers yet, this line is the header
                if (!string.IsNullOrEmpty(currentSection) && headers == null)
                {
                    headers = ParseCsvLine(line);
                    continue;
                }

                // Skip if no section or headers
                if (string.IsNullOrEmpty(currentSection) || headers == null) continue;

                try
                {
                    var values = ParseCsvLine(line);
                    if (values.Length == 0) continue;

                    switch (currentSection)
                    {
                        case "CUSTOMERS":
                            await ImportCustomerRowAsync(headers, values, result);
                            break;
                        case "INVENTORY":
                            await ImportInventoryRowAsync(headers, values, result);
                            break;
                        // Add more sections as needed
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Line {lineNum} ({currentSection}): {ex.Message}");
                }
            }

            await _db.SaveChangesAsync();
            result.Success = result.Errors.Count == 0 || (result.Inserted + result.Updated) > 0;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Import failed: {ex.Message}");
        }

        return result;
    }

    private async Task ImportCustomerRowAsync(string[] headers, string[] values, CsvImportResult result)
    {
        var nameIndex = Array.FindIndex(headers, h => h.Equals("Name", StringComparison.OrdinalIgnoreCase));
        if (nameIndex < 0) return;

        var name = GetValue(values, nameIndex);
        if (string.IsNullOrWhiteSpace(name)) return;

        var emailIndex = Array.FindIndex(headers, h => h.Equals("Email", StringComparison.OrdinalIgnoreCase));
        var email = GetValue(values, emailIndex);

        var existing = await _db.Customers.FirstOrDefaultAsync(c => c.Name == name);
        if (existing != null)
        {
            // Update
            existing.Email = email ?? existing.Email;
            existing.Phone = GetValue(values, Array.FindIndex(headers, h => h.Equals("Phone", StringComparison.OrdinalIgnoreCase))) ?? existing.Phone;
            existing.CompanyName = GetValue(values, Array.FindIndex(headers, h => h.Equals("CompanyName", StringComparison.OrdinalIgnoreCase))) ?? existing.CompanyName;
            result.Updated++;
        }
        else
        {
            // Insert
            _db.Customers.Add(new Customer
            {
                Name = name,
                Email = email,
                Phone = GetValue(values, Array.FindIndex(headers, h => h.Equals("Phone", StringComparison.OrdinalIgnoreCase))),
                CompanyName = GetValue(values, Array.FindIndex(headers, h => h.Equals("CompanyName", StringComparison.OrdinalIgnoreCase))),
                CreatedAt = DateTime.UtcNow
            });
            result.Inserted++;
        }
    }

    private async Task ImportInventoryRowAsync(string[] headers, string[] values, CsvImportResult result)
    {
        var nameIndex = Array.FindIndex(headers, h => h.Equals("Name", StringComparison.OrdinalIgnoreCase));
        if (nameIndex < 0) return;

        var name = GetValue(values, nameIndex);
        if (string.IsNullOrWhiteSpace(name)) return;

        var skuIndex = Array.FindIndex(headers, h => h.Equals("Sku", StringComparison.OrdinalIgnoreCase));
        var sku = GetValue(values, skuIndex);

        var existing = await _db.InventoryItems.FirstOrDefaultAsync(i => i.Name == name || (sku != null && i.Sku == sku));
        if (existing != null)
        {
            // Update quantity and price
            var qtyIndex = Array.FindIndex(headers, h => h.Equals("QuantityOnHand", StringComparison.OrdinalIgnoreCase));
            var priceIndex = Array.FindIndex(headers, h => h.Equals("Price", StringComparison.OrdinalIgnoreCase));
            
            if (qtyIndex >= 0 && decimal.TryParse(GetValue(values, qtyIndex), out var qty))
                existing.QuantityOnHand = qty;
            if (priceIndex >= 0 && decimal.TryParse(GetValue(values, priceIndex), out var price))
                existing.Price = price;
            
            result.Updated++;
        }
        else
        {
            // Insert new
            var item = new InventoryItem { Name = name, Sku = sku ?? $"SKU-{DateTime.Now.Ticks}", CreatedAt = DateTime.UtcNow };
            
            var priceIndex = Array.FindIndex(headers, h => h.Equals("Price", StringComparison.OrdinalIgnoreCase));
            if (priceIndex >= 0 && decimal.TryParse(GetValue(values, priceIndex), out var price))
                item.Price = price;
                
            _db.InventoryItems.Add(item);
            result.Inserted++;
        }
    }

    #endregion

    #region Import Methods

    /// <summary>
    /// Imports customers from CSV with merge/upsert logic.
    /// </summary>
    public async Task<CsvImportResult> ImportCustomersAsync(string filePath)
    {
        var result = new CsvImportResult { EntityType = "Customers" };
        
        try
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            if (lines.Length < 2)
            {
                result.Errors.Add("CSV file is empty or has no data rows.");
                return result;
            }

            var headers = ParseCsvLine(lines[0]);
            var nameIndex = Array.FindIndex(headers, h => h.Equals("Name", StringComparison.OrdinalIgnoreCase));
            var emailIndex = Array.FindIndex(headers, h => h.Equals("Email", StringComparison.OrdinalIgnoreCase));
            var phoneIndex = Array.FindIndex(headers, h => h.Equals("Phone", StringComparison.OrdinalIgnoreCase));
            var companyIndex = Array.FindIndex(headers, h => h.Equals("CompanyName", StringComparison.OrdinalIgnoreCase));
            var notesIndex = Array.FindIndex(headers, h => h.Equals("Notes", StringComparison.OrdinalIgnoreCase));

            if (nameIndex < 0)
            {
                result.Errors.Add("Required column 'Name' not found.");
                return result;
            }

            for (int i = 1; i < lines.Length; i++)
            {
                try
                {
                    var values = ParseCsvLine(lines[i]);
                    if (values.Length == 0) continue;

                    var name = GetValue(values, nameIndex);
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var email = GetValue(values, emailIndex);

                    // Find existing by name+email or just name
                    var existing = await _db.Customers.FirstOrDefaultAsync(c => 
                        c.Name == name && (c.Email == email || string.IsNullOrEmpty(email)));

                    if (existing != null)
                    {
                        // Update existing
                        existing.Phone = GetValue(values, phoneIndex) ?? existing.Phone;
                        existing.CompanyName = GetValue(values, companyIndex) ?? existing.CompanyName;
                        existing.Notes = GetValue(values, notesIndex) ?? existing.Notes;
                        result.Updated++;
                    }
                    else
                    {
                        // Insert new
                        var customer = new Customer
                        {
                            Name = name,
                            Email = email,
                            Phone = GetValue(values, phoneIndex),
                            CompanyName = GetValue(values, companyIndex),
                            Notes = GetValue(values, notesIndex),
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.Customers.Add(customer);
                        result.Inserted++;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Row {i + 1}: {ex.Message}");
                }
            }

            await _db.SaveChangesAsync();
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Import failed: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Imports inventory items from CSV with merge/upsert logic.
    /// </summary>
    public async Task<CsvImportResult> ImportInventoryAsync(string filePath)
    {
        var result = new CsvImportResult { EntityType = "Inventory" };
        
        try
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            if (lines.Length < 2)
            {
                result.Errors.Add("CSV file is empty or has no data rows.");
                return result;
            }

            var headers = ParseCsvLine(lines[0]);
            var skuIndex = Array.FindIndex(headers, h => h.Equals("Sku", StringComparison.OrdinalIgnoreCase));
            var nameIndex = Array.FindIndex(headers, h => h.Equals("Name", StringComparison.OrdinalIgnoreCase));
            var qtyIndex = Array.FindIndex(headers, h => h.Equals("QuantityOnHand", StringComparison.OrdinalIgnoreCase));
            var costIndex = Array.FindIndex(headers, h => h.Equals("Cost", StringComparison.OrdinalIgnoreCase));
            var priceIndex = Array.FindIndex(headers, h => h.Equals("Price", StringComparison.OrdinalIgnoreCase));
            var reorderIndex = Array.FindIndex(headers, h => h.Equals("ReorderPoint", StringComparison.OrdinalIgnoreCase));

            if (nameIndex < 0)
            {
                result.Errors.Add("Required column 'Name' not found.");
                return result;
            }

            for (int i = 1; i < lines.Length; i++)
            {
                try
                {
                    var values = ParseCsvLine(lines[i]);
                    if (values.Length == 0) continue;

                    var name = GetValue(values, nameIndex);
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var sku = GetValue(values, skuIndex);

                    // Find existing by SKU or name
                    var existing = await _db.InventoryItems.FirstOrDefaultAsync(inv => 
                        (!string.IsNullOrEmpty(sku) && inv.Sku == sku) || inv.Name == name);

                    if (existing != null)
                    {
                        // Update existing
                        if (decimal.TryParse(GetValue(values, qtyIndex), out var qty))
                            existing.QuantityOnHand = qty;
                        if (decimal.TryParse(GetValue(values, costIndex), out var cost))
                            existing.Cost = cost;
                        if (decimal.TryParse(GetValue(values, priceIndex), out var price))
                            existing.Price = price;
                        if (decimal.TryParse(GetValue(values, reorderIndex), out var reorder))
                            existing.ReorderPoint = reorder;
                        result.Updated++;
                    }
                    else
                    {
                        // Insert new
                        var item = new InventoryItem
                        {
                            Name = name,
                            Sku = sku ?? $"SKU-{DateTime.Now.Ticks}",
                            QuantityOnHand = decimal.TryParse(GetValue(values, qtyIndex), out var qty) ? qty : 0,
                            Cost = decimal.TryParse(GetValue(values, costIndex), out var cost) ? cost : 0,
                            Price = decimal.TryParse(GetValue(values, priceIndex), out var price) ? price : 0,
                            ReorderPoint = decimal.TryParse(GetValue(values, reorderIndex), out var reorder) ? reorder : 5,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.InventoryItems.Add(item);
                        result.Inserted++;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Row {i + 1}: {ex.Message}");
                }
            }

            await _db.SaveChangesAsync();
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Import failed: {ex.Message}");
        }

        return result;
    }

    #endregion

    #region Helpers

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString().Trim());
        return result.ToArray();
    }

    private static string? GetValue(string[] values, int index)
    {
        if (index < 0 || index >= values.Length) return null;
        var value = values[index].Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    /// <summary>
    /// Shares an exported CSV file via native share sheet.
    /// </summary>
    public async Task<bool> ShareCsvAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return false;

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Share OneManVan Export",
                File = new ShareFile(filePath)
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets list of available exported CSV files.
    /// </summary>
    public async Task<IEnumerable<CsvFileInfo>> GetExportedFilesAsync()
    {
        var files = new List<CsvFileInfo>();

        try
        {
            if (Directory.Exists(_exportDirectory))
            {
                foreach (var file in Directory.GetFiles(_exportDirectory, "*.csv").OrderByDescending(f => File.GetCreationTime(f)))
                {
                    var info = new FileInfo(file);
                    files.Add(new CsvFileInfo
                    {
                        FilePath = file,
                        FileName = info.Name,
                        CreatedAt = info.CreationTime,
                        FileSizeBytes = info.Length
                    });
                }
            }
        }
        catch { }

        return await Task.FromResult(files);
    }

    #endregion
}

/// <summary>
/// Result of a CSV export operation.
/// </summary>
public class CsvExportResult
{
    public bool Success { get; set; }
    public string? FilePath { get; set; }
    public string? EntityType { get; set; }
    public int RecordCount { get; set; }
    public string? Message { get; set; }

    public static CsvExportResult Succeeded(string filePath, int count, string entityType)
        => new() { Success = true, FilePath = filePath, RecordCount = count, EntityType = entityType, Message = $"Exported {count} {entityType}" };

    public static CsvExportResult Failed(string message)
        => new() { Success = false, Message = message };
}

/// <summary>
/// Result of a CSV import operation.
/// </summary>
public class CsvImportResult
{
    public bool Success { get; set; }
    public string EntityType { get; set; } = "";
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int TotalProcessed => Inserted + Updated;
    public List<string> Errors { get; set; } = [];
}

/// <summary>
/// Information about an exported CSV file.
/// </summary>
public class CsvFileInfo
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public long FileSizeBytes { get; set; }
    public string FileSizeDisplay => FileSizeBytes < 1024 ? $"{FileSizeBytes} B" : $"{FileSizeBytes / 1024.0:F1} KB";
}
