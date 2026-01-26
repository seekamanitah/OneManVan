using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;

namespace OneManVan.Services;

/// <summary>
/// Service for exporting and importing data as Excel files.
/// Excel files can be edited in Microsoft Excel and re-imported.
/// </summary>
public class ExcelExportImportService
{
    private readonly OneManVanDbContext _context;

    public ExcelExportImportService(OneManVanDbContext context)
    {
        _context = context;
    }

    #region Export Methods

    /// <summary>
    /// Exports all data to a multi-sheet Excel workbook.
    /// </summary>
    public async Task<ExcelExportResult> ExportAllToExcelAsync(string filePath)
    {
        try
        {
            using var workbook = new XLWorkbook();
            var totalRecords = 0;

            // Customers sheet
            var customers = await _context.Customers.OrderBy(c => c.Name).ToListAsync();
            var customersSheet = workbook.Worksheets.Add("Customers");
            customersSheet.Cell(1, 1).Value = "Id";
            customersSheet.Cell(1, 2).Value = "Name";
            customersSheet.Cell(1, 3).Value = "CompanyName";
            customersSheet.Cell(1, 4).Value = "Email";
            customersSheet.Cell(1, 5).Value = "Phone";
            customersSheet.Cell(1, 6).Value = "CustomerType";
            customersSheet.Cell(1, 7).Value = "Status";
            customersSheet.Cell(1, 8).Value = "Notes";

            for (int i = 0; i < customers.Count; i++)
            {
                var c = customers[i];
                customersSheet.Cell(i + 2, 1).Value = c.Id;
                customersSheet.Cell(i + 2, 2).Value = c.Name;
                customersSheet.Cell(i + 2, 3).Value = c.CompanyName ?? "";
                customersSheet.Cell(i + 2, 4).Value = c.Email ?? "";
                customersSheet.Cell(i + 2, 5).Value = c.Phone ?? "";
                customersSheet.Cell(i + 2, 6).Value = c.CustomerType.ToString();
                customersSheet.Cell(i + 2, 7).Value = c.Status.ToString();
                customersSheet.Cell(i + 2, 8).Value = c.Notes ?? "";
            }
            customersSheet.Columns().AdjustToContents();
            customersSheet.Row(1).Style.Font.Bold = true;
            totalRecords += customers.Count;

            // Inventory sheet
            var inventory = await _context.InventoryItems.Where(i => i.IsActive).OrderBy(i => i.Name).ToListAsync();
            var inventorySheet = workbook.Worksheets.Add("Inventory");
            inventorySheet.Cell(1, 1).Value = "Id";
            inventorySheet.Cell(1, 2).Value = "Sku";
            inventorySheet.Cell(1, 3).Value = "Name";
            inventorySheet.Cell(1, 4).Value = "Description";
            inventorySheet.Cell(1, 5).Value = "Category";
            inventorySheet.Cell(1, 6).Value = "QuantityOnHand";
            inventorySheet.Cell(1, 7).Value = "ReorderPoint";
            inventorySheet.Cell(1, 8).Value = "Cost";
            inventorySheet.Cell(1, 9).Value = "Price";
            inventorySheet.Cell(1, 10).Value = "Unit";
            inventorySheet.Cell(1, 11).Value = "Location";
            inventorySheet.Cell(1, 12).Value = "Supplier";

            for (int i = 0; i < inventory.Count; i++)
            {
                var item = inventory[i];
                inventorySheet.Cell(i + 2, 1).Value = item.Id;
                inventorySheet.Cell(i + 2, 2).Value = item.Sku ?? "";
                inventorySheet.Cell(i + 2, 3).Value = item.Name ?? "";
                inventorySheet.Cell(i + 2, 4).Value = item.Description ?? "";
                inventorySheet.Cell(i + 2, 5).Value = item.Category.ToString();
                inventorySheet.Cell(i + 2, 6).Value = (double)item.QuantityOnHand;
                inventorySheet.Cell(i + 2, 7).Value = (double)item.ReorderPoint;
                inventorySheet.Cell(i + 2, 8).Value = (double)item.Cost;
                inventorySheet.Cell(i + 2, 9).Value = (double)item.Price;
                inventorySheet.Cell(i + 2, 10).Value = item.Unit ?? "";
                inventorySheet.Cell(i + 2, 11).Value = item.Location ?? "";
                inventorySheet.Cell(i + 2, 12).Value = item.Supplier ?? "";
            }
            inventorySheet.Columns().AdjustToContents();
            inventorySheet.Row(1).Style.Font.Bold = true;
            totalRecords += inventory.Count;

            // Assets sheet
            var assets = await _context.Assets.Include(a => a.Customer).OrderBy(a => a.Customer!.Name).ToListAsync();
            var assetsSheet = workbook.Worksheets.Add("Assets");
            assetsSheet.Cell(1, 1).Value = "Id";
            assetsSheet.Cell(1, 2).Value = "CustomerName";
            assetsSheet.Cell(1, 3).Value = "Nickname";
            assetsSheet.Cell(1, 4).Value = "Brand";
            assetsSheet.Cell(1, 5).Value = "Model";
            assetsSheet.Cell(1, 6).Value = "Serial";
            assetsSheet.Cell(1, 7).Value = "EquipmentType";
            assetsSheet.Cell(1, 8).Value = "FuelType";
            assetsSheet.Cell(1, 9).Value = "RefrigerantType";
            assetsSheet.Cell(1, 10).Value = "TonnageX10";
            assetsSheet.Cell(1, 11).Value = "SeerRating";
            assetsSheet.Cell(1, 12).Value = "InstallDate";
            assetsSheet.Cell(1, 13).Value = "Status";

            for (int i = 0; i < assets.Count; i++)
            {
                var a = assets[i];
                assetsSheet.Cell(i + 2, 1).Value = a.Id;
                assetsSheet.Cell(i + 2, 2).Value = a.Customer?.Name ?? "";
                assetsSheet.Cell(i + 2, 3).Value = a.Nickname ?? "";
                assetsSheet.Cell(i + 2, 4).Value = a.Brand ?? "";
                assetsSheet.Cell(i + 2, 5).Value = a.Model ?? "";
                assetsSheet.Cell(i + 2, 6).Value = a.Serial ?? "";
                assetsSheet.Cell(i + 2, 7).Value = a.EquipmentType.ToString();
                assetsSheet.Cell(i + 2, 8).Value = a.FuelType.ToString();
                assetsSheet.Cell(i + 2, 9).Value = a.RefrigerantType.ToString();
                assetsSheet.Cell(i + 2, 10).Value = a.TonnageX10;
                assetsSheet.Cell(i + 2, 11).Value = (double)(a.SeerRating ?? 0);
                assetsSheet.Cell(i + 2, 12).Value = a.InstallDate?.ToString("yyyy-MM-dd") ?? "";
                assetsSheet.Cell(i + 2, 13).Value = a.Status.ToString();
            }
            assetsSheet.Columns().AdjustToContents();
            assetsSheet.Row(1).Style.Font.Bold = true;
            totalRecords += assets.Count;

            // Jobs sheet
            var jobs = await _context.Jobs.Include(j => j.Customer).OrderByDescending(j => j.ScheduledDate).ToListAsync();
            var jobsSheet = workbook.Worksheets.Add("Jobs");
            jobsSheet.Cell(1, 1).Value = "Id";
            jobsSheet.Cell(1, 2).Value = "CustomerName";
            jobsSheet.Cell(1, 3).Value = "Title";
            jobsSheet.Cell(1, 4).Value = "Status";
            jobsSheet.Cell(1, 5).Value = "ScheduledDate";
            jobsSheet.Cell(1, 6).Value = "CompletedAt";
            jobsSheet.Cell(1, 7).Value = "LaborTotal";
            jobsSheet.Cell(1, 8).Value = "PartsTotal";
            jobsSheet.Cell(1, 9).Value = "Total";

            for (int i = 0; i < jobs.Count; i++)
            {
                var j = jobs[i];
                jobsSheet.Cell(i + 2, 1).Value = j.Id;
                jobsSheet.Cell(i + 2, 2).Value = j.Customer?.Name ?? "";
                jobsSheet.Cell(i + 2, 3).Value = j.Title ?? "";
                jobsSheet.Cell(i + 2, 4).Value = j.Status.ToString();
                jobsSheet.Cell(i + 2, 5).Value = j.ScheduledDate?.ToString("yyyy-MM-dd HH:mm") ?? "";
                jobsSheet.Cell(i + 2, 6).Value = j.CompletedAt?.ToString("yyyy-MM-dd HH:mm") ?? "";
                jobsSheet.Cell(i + 2, 7).Value = (double)j.LaborTotal;
                jobsSheet.Cell(i + 2, 8).Value = (double)j.PartsTotal;
                jobsSheet.Cell(i + 2, 9).Value = (double)j.Total;
            }
            jobsSheet.Columns().AdjustToContents();
            jobsSheet.Row(1).Style.Font.Bold = true;
            totalRecords += jobs.Count;

            // Invoices sheet
            var invoices = await _context.Invoices.Include(i => i.Customer).OrderByDescending(i => i.CreatedAt).ToListAsync();
            var invoicesSheet = workbook.Worksheets.Add("Invoices");
            invoicesSheet.Cell(1, 1).Value = "Id";
            invoicesSheet.Cell(1, 2).Value = "InvoiceNumber";
            invoicesSheet.Cell(1, 3).Value = "CustomerName";
            invoicesSheet.Cell(1, 4).Value = "Status";
            invoicesSheet.Cell(1, 5).Value = "SubTotal";
            invoicesSheet.Cell(1, 6).Value = "TaxAmount";
            invoicesSheet.Cell(1, 7).Value = "Total";
            invoicesSheet.Cell(1, 8).Value = "AmountPaid";
            invoicesSheet.Cell(1, 9).Value = "BalanceDue";
            invoicesSheet.Cell(1, 10).Value = "DueDate";

            for (int i = 0; i < invoices.Count; i++)
            {
                var inv = invoices[i];
                invoicesSheet.Cell(i + 2, 1).Value = inv.Id;
                invoicesSheet.Cell(i + 2, 2).Value = inv.InvoiceNumber ?? "";
                invoicesSheet.Cell(i + 2, 3).Value = inv.Customer?.Name ?? "";
                invoicesSheet.Cell(i + 2, 4).Value = inv.Status.ToString();
                invoicesSheet.Cell(i + 2, 5).Value = (double)inv.SubTotal;
                invoicesSheet.Cell(i + 2, 6).Value = (double)inv.TaxAmount;
                invoicesSheet.Cell(i + 2, 7).Value = (double)inv.Total;
                invoicesSheet.Cell(i + 2, 8).Value = (double)inv.AmountPaid;
                invoicesSheet.Cell(i + 2, 9).Value = (double)inv.BalanceDue;
                invoicesSheet.Cell(i + 2, 10).Value = inv.DueDate.ToString("yyyy-MM-dd");
            }
            invoicesSheet.Columns().AdjustToContents();
            invoicesSheet.Row(1).Style.Font.Bold = true;
            totalRecords += invoices.Count;

            workbook.SaveAs(filePath);

            return new ExcelExportResult
            {
                Success = true,
                FilePath = filePath,
                RecordCount = totalRecords,
                SheetCount = 5,
                Message = $"Exported {totalRecords} records to 5 sheets"
            };
        }
        catch (Exception ex)
        {
            return new ExcelExportResult
            {
                Success = false,
                Message = $"Export failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Exports only customers to an Excel file.
    /// </summary>
    public async Task<ExcelExportResult> ExportCustomersToExcelAsync(string filePath)
    {
        try
        {
            using var workbook = new XLWorkbook();
            var customers = await _context.Customers.OrderBy(c => c.Name).ToListAsync();
            
            var sheet = workbook.Worksheets.Add("Customers");
            
            // Headers
            sheet.Cell(1, 1).Value = "Id";
            sheet.Cell(1, 2).Value = "Name";
            sheet.Cell(1, 3).Value = "CompanyName";
            sheet.Cell(1, 4).Value = "Email";
            sheet.Cell(1, 5).Value = "Phone";
            sheet.Cell(1, 6).Value = "CustomerType";
            sheet.Cell(1, 7).Value = "Status";
            sheet.Cell(1, 8).Value = "Notes";
            
            for (int i = 0; i < customers.Count; i++)
            {
                var c = customers[i];
                sheet.Cell(i + 2, 1).Value = c.Id;
                sheet.Cell(i + 2, 2).Value = c.Name;
                sheet.Cell(i + 2, 3).Value = c.CompanyName ?? "";
                sheet.Cell(i + 2, 4).Value = c.Email ?? "";
                sheet.Cell(i + 2, 5).Value = c.Phone ?? "";
                sheet.Cell(i + 2, 6).Value = c.CustomerType.ToString();
                sheet.Cell(i + 2, 7).Value = c.Status.ToString();
                sheet.Cell(i + 2, 8).Value = c.Notes ?? "";
            }

            sheet.Columns().AdjustToContents();
            sheet.Row(1).Style.Font.Bold = true;
            sheet.SheetView.FreezeRows(1);

            workbook.SaveAs(filePath);

            return new ExcelExportResult
            {
                Success = true,
                FilePath = filePath,
                RecordCount = customers.Count,
                SheetCount = 1,
                Message = $"Exported {customers.Count} customers"
            };
        }
        catch (Exception ex)
        {
            return new ExcelExportResult
            {
                Success = false,
                Message = $"Export failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Exports only inventory to an Excel file.
    /// </summary>
    public async Task<ExcelExportResult> ExportInventoryToExcelAsync(string filePath)
    {
        try
        {
            using var workbook = new XLWorkbook();
            var items = await _context.InventoryItems.Where(i => i.IsActive).OrderBy(i => i.Name).ToListAsync();
            
            var sheet = workbook.Worksheets.Add("Inventory");
            
            // Headers
            sheet.Cell(1, 1).Value = "Id";
            sheet.Cell(1, 2).Value = "Sku";
            sheet.Cell(1, 3).Value = "Name";
            sheet.Cell(1, 4).Value = "Description";
            sheet.Cell(1, 5).Value = "Category";
            sheet.Cell(1, 6).Value = "QuantityOnHand";
            sheet.Cell(1, 7).Value = "ReorderPoint";
            sheet.Cell(1, 8).Value = "Cost";
            sheet.Cell(1, 9).Value = "Price";
            sheet.Cell(1, 10).Value = "Unit";
            sheet.Cell(1, 11).Value = "Location";
            sheet.Cell(1, 12).Value = "Supplier";
            sheet.Cell(1, 13).Value = "PartNumber";
            
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                sheet.Cell(i + 2, 1).Value = item.Id;
                sheet.Cell(i + 2, 2).Value = item.Sku ?? "";
                sheet.Cell(i + 2, 3).Value = item.Name ?? "";
                sheet.Cell(i + 2, 4).Value = item.Description ?? "";
                sheet.Cell(i + 2, 5).Value = item.Category.ToString();
                sheet.Cell(i + 2, 6).Value = (double)item.QuantityOnHand;
                sheet.Cell(i + 2, 7).Value = (double)item.ReorderPoint;
                sheet.Cell(i + 2, 8).Value = (double)item.Cost;
                sheet.Cell(i + 2, 9).Value = (double)item.Price;
                sheet.Cell(i + 2, 10).Value = item.Unit ?? "";
                sheet.Cell(i + 2, 11).Value = item.Location ?? "";
                sheet.Cell(i + 2, 12).Value = item.Supplier ?? "";
                sheet.Cell(i + 2, 13).Value = item.PartNumber ?? "";
            }

            sheet.Columns().AdjustToContents();
            sheet.Row(1).Style.Font.Bold = true;
            sheet.SheetView.FreezeRows(1);

            // Format currency columns
            sheet.Column(8).Style.NumberFormat.Format = "$#,##0.00";
            sheet.Column(9).Style.NumberFormat.Format = "$#,##0.00";

            workbook.SaveAs(filePath);

            return new ExcelExportResult
            {
                Success = true,
                FilePath = filePath,
                RecordCount = items.Count,
                SheetCount = 1,
                Message = $"Exported {items.Count} inventory items"
            };
        }
        catch (Exception ex)
        {
            return new ExcelExportResult
            {
                Success = false,
                Message = $"Export failed: {ex.Message}"
            };
        }
    }

    #endregion

    #region Import Methods

    /// <summary>
    /// Imports customers from an Excel file with merge/upsert logic.
    /// </summary>
    public async Task<ExcelImportResult> ImportCustomersFromExcelAsync(string filePath)
    {
        var result = new ExcelImportResult { EntityType = "Customers" };

        try
        {
            using var workbook = new XLWorkbook(filePath);
            var sheet = workbook.Worksheets.FirstOrDefault(ws => 
                ws.Name.Equals("Customers", StringComparison.OrdinalIgnoreCase)) 
                ?? workbook.Worksheets.First();

            var headerRow = sheet.Row(1);
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            
            var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 1;
            for (int col = 1; col <= lastCol; col++)
            {
                var header = headerRow.Cell(col).GetString().Trim();
                if (!string.IsNullOrEmpty(header))
                    headers[header] = col;
            }

            if (!headers.ContainsKey("Name"))
            {
                result.Errors.Add("Required column 'Name' not found.");
                return result;
            }

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

            for (int row = 2; row <= lastRow; row++)
            {
                try
                {
                    var name = GetCellValue(sheet, row, headers, "Name");
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var email = GetCellValue(sheet, row, headers, "Email");

                    var existing = await _context.Customers.FirstOrDefaultAsync(c => 
                        c.Name == name && (c.Email == email || string.IsNullOrEmpty(email)));

                    if (existing != null)
                    {
                        existing.Phone = GetCellValue(sheet, row, headers, "Phone") ?? existing.Phone;
                        existing.CompanyName = GetCellValue(sheet, row, headers, "CompanyName") ?? existing.CompanyName;
                        existing.Notes = GetCellValue(sheet, row, headers, "Notes") ?? existing.Notes;
                        result.Updated++;
                    }
                    else
                    {
                        var customer = new Shared.Models.Customer
                        {
                            Name = name,
                            Email = email,
                            Phone = GetCellValue(sheet, row, headers, "Phone"),
                            CompanyName = GetCellValue(sheet, row, headers, "CompanyName"),
                            Notes = GetCellValue(sheet, row, headers, "Notes"),
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Customers.Add(customer);
                        result.Inserted++;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Row {row}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Import failed: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Imports inventory items from an Excel file with merge/upsert logic.
    /// </summary>
    public async Task<ExcelImportResult> ImportInventoryFromExcelAsync(string filePath)
    {
        var result = new ExcelImportResult { EntityType = "Inventory" };

        try
        {
            using var workbook = new XLWorkbook(filePath);
            var sheet = workbook.Worksheets.FirstOrDefault(ws => 
                ws.Name.Equals("Inventory", StringComparison.OrdinalIgnoreCase)) 
                ?? workbook.Worksheets.First();

            var headerRow = sheet.Row(1);
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            
            var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 1;
            for (int col = 1; col <= lastCol; col++)
            {
                var header = headerRow.Cell(col).GetString().Trim();
                if (!string.IsNullOrEmpty(header))
                    headers[header] = col;
            }

            if (!headers.ContainsKey("Name"))
            {
                result.Errors.Add("Required column 'Name' not found.");
                return result;
            }

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

            for (int row = 2; row <= lastRow; row++)
            {
                try
                {
                    var name = GetCellValue(sheet, row, headers, "Name");
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var sku = GetCellValue(sheet, row, headers, "Sku");

                    var existing = await _context.InventoryItems.FirstOrDefaultAsync(i => 
                        (!string.IsNullOrEmpty(sku) && i.Sku == sku) || i.Name == name);

                    if (existing != null)
                    {
                        if (decimal.TryParse(GetCellValue(sheet, row, headers, "QuantityOnHand"), out var qty))
                            existing.QuantityOnHand = qty;
                        if (decimal.TryParse(GetCellValue(sheet, row, headers, "Cost"), out var cost))
                            existing.Cost = cost;
                        if (decimal.TryParse(GetCellValue(sheet, row, headers, "Price"), out var price))
                            existing.Price = price;
                        if (decimal.TryParse(GetCellValue(sheet, row, headers, "ReorderPoint"), out var reorder))
                            existing.ReorderPoint = reorder;
                        existing.Description = GetCellValue(sheet, row, headers, "Description") ?? existing.Description;
                        existing.Location = GetCellValue(sheet, row, headers, "Location") ?? existing.Location;
                        result.Updated++;
                    }
                    else
                    {
                        var item = new Shared.Models.InventoryItem
                        {
                            Name = name,
                            Sku = sku ?? $"SKU-{DateTime.Now.Ticks}",
                            Description = GetCellValue(sheet, row, headers, "Description"),
                            QuantityOnHand = decimal.TryParse(GetCellValue(sheet, row, headers, "QuantityOnHand"), out var qty) ? qty : 0,
                            Cost = decimal.TryParse(GetCellValue(sheet, row, headers, "Cost"), out var cost) ? cost : 0,
                            Price = decimal.TryParse(GetCellValue(sheet, row, headers, "Price"), out var price) ? price : 0,
                            ReorderPoint = decimal.TryParse(GetCellValue(sheet, row, headers, "ReorderPoint"), out var reorder) ? reorder : 5,
                            Unit = GetCellValue(sheet, row, headers, "Unit") ?? "ea",
                            Location = GetCellValue(sheet, row, headers, "Location"),
                            Supplier = GetCellValue(sheet, row, headers, "Supplier"),
                            PartNumber = GetCellValue(sheet, row, headers, "PartNumber"),
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.InventoryItems.Add(item);
                        result.Inserted++;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Row {row}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
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

    private static string? GetCellValue(IXLWorksheet sheet, int row, Dictionary<string, int> headers, string columnName)
    {
        if (!headers.TryGetValue(columnName, out var col)) return null;
        var value = sheet.Cell(row, col).GetString().Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    #endregion
}

/// <summary>
/// Result of an Excel export operation.
/// </summary>
public class ExcelExportResult
{
    public bool Success { get; set; }
    public string? FilePath { get; set; }
    public int RecordCount { get; set; }
    public int SheetCount { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Result of an Excel import operation.
/// </summary>
public class ExcelImportResult
{
    public bool Success { get; set; }
    public string EntityType { get; set; } = "";
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int TotalProcessed => Inserted + Updated;
    public List<string> Errors { get; set; } = [];
}
