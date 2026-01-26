namespace OneManVan.Services;

/// <summary>
/// Service for exporting and importing OneManVan data backups.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Export the entire database to a JSON file.
    /// </summary>
    Task<string> ExportToJsonAsync(string filePath);

    /// <summary>
    /// Export the entire database to a compressed ZIP file.
    /// </summary>
    Task<string> ExportToZipAsync(string filePath);

    /// <summary>
    /// Export a subset of data (e.g., HVAC assets only) to JSON.
    /// </summary>
    Task<string> ExportSubsetAsync(string filePath, ExportOptions options);

    /// <summary>
    /// Import data from a JSON backup file with merge/upsert logic.
    /// </summary>
    Task<ImportResult> ImportFromJsonAsync(string filePath);

    /// <summary>
    /// Import data from a compressed ZIP backup file.
    /// </summary>
    Task<ImportResult> ImportFromZipAsync(string filePath);

    /// <summary>
    /// Get the path to the current SQLite database file.
    /// </summary>
    string GetDatabasePath();
}

/// <summary>
/// Options for subset export.
/// </summary>
public class ExportOptions
{
    public bool IncludeCustomers { get; set; } = true;
    public bool IncludeSites { get; set; } = true;
    public bool IncludeAssets { get; set; } = true;
    public bool IncludeCustomFields { get; set; } = true;
    public bool IncludeEstimates { get; set; } = true;
    public bool IncludeInventory { get; set; } = true;
    public bool IncludeJobs { get; set; } = true;
    public bool IncludeInvoices { get; set; } = true;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

/// <summary>
/// Result of an import operation.
/// </summary>
public class ImportResult
{
    public bool Success { get; set; }
    
    // Phase 1 counts
    public int CustomersImported { get; set; }
    public int SitesImported { get; set; }
    public int AssetsImported { get; set; }
    public int CustomFieldsImported { get; set; }
    
    // Phase 2 counts
    public int EstimatesImported { get; set; }
    public int InventoryImported { get; set; }
    
    // Phase 3 counts
    public int JobsImported { get; set; }
    public int InvoicesImported { get; set; }
    public int PaymentsImported { get; set; }
    
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    
    public int TotalImported => CustomersImported + SitesImported + AssetsImported + 
        CustomFieldsImported + EstimatesImported + InventoryImported +
        JobsImported + InvoicesImported + PaymentsImported;

    /// <summary>
    /// Gets a summary of what was imported.
    /// </summary>
    public string Summary
    {
        get
        {
            var parts = new List<string>();
            if (CustomersImported > 0) parts.Add($"{CustomersImported} customers");
            if (SitesImported > 0) parts.Add($"{SitesImported} sites");
            if (AssetsImported > 0) parts.Add($"{AssetsImported} assets");
            if (EstimatesImported > 0) parts.Add($"{EstimatesImported} estimates");
            if (InventoryImported > 0) parts.Add($"{InventoryImported} inventory items");
            if (JobsImported > 0) parts.Add($"{JobsImported} jobs");
            if (InvoicesImported > 0) parts.Add($"{InvoicesImported} invoices");
            if (PaymentsImported > 0) parts.Add($"{PaymentsImported} payments");
            
            return parts.Count > 0 ? string.Join(", ", parts) : "Nothing imported";
        }
    }
}
