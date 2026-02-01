using OneManVan.Shared.Models;

namespace OneManVan.Web.Services.Import;

/// <summary>
/// Result of an import operation
/// </summary>
public class ImportResult
{
    public bool Success { get; set; }
    public int TotalRecords { get; set; }
    public int ImportedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<ImportError> Errors { get; set; } = new();
    public List<ImportWarning> Warnings { get; set; } = new();
    public string Message => Success 
        ? $"Successfully imported {ImportedCount} records, updated {UpdatedCount}, skipped {SkippedCount}."
        : $"Import failed with {ErrorCount} errors.";
}

public class ImportError
{
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string RawValue { get; set; } = string.Empty;
}

public class ImportWarning
{
    public int RowNumber { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Options for import behavior
/// </summary>
public class ImportOptions
{
    /// <summary>
    /// If true, existing records will be updated. If false, they will be skipped.
    /// </summary>
    public bool UpdateExisting { get; set; } = false;
    
    /// <summary>
    /// If true, import will stop on first error. If false, it will continue and report all errors.
    /// </summary>
    public bool StopOnError { get; set; } = false;
    
    /// <summary>
    /// If true, validates data without actually importing.
    /// </summary>
    public bool ValidateOnly { get; set; } = false;
    
    /// <summary>
    /// The field to use for duplicate detection (e.g., "Email", "Name", "InvoiceNumber")
    /// </summary>
    public string? DuplicateCheckField { get; set; }
}

/// <summary>
/// Preview of data to be imported
/// </summary>
public class ImportPreview<T>
{
    public List<T> NewRecords { get; set; } = new();
    public List<T> ExistingRecords { get; set; } = new();
    public List<ImportError> ValidationErrors { get; set; } = new();
    public int TotalRows { get; set; }
    public bool IsValid => ValidationErrors.Count == 0;
}

/// <summary>
/// Interface for CSV import operations
/// </summary>
public interface ICsvImportService
{
    // Customer imports
    Task<ImportPreview<Customer>> PreviewCustomersFromCsvAsync(Stream csvStream);
    Task<ImportResult> ImportCustomersFromCsvAsync(Stream csvStream, ImportOptions options);
    
    // Invoice imports
    Task<ImportPreview<Invoice>> PreviewInvoicesFromCsvAsync(Stream csvStream);
    Task<ImportResult> ImportInvoicesFromCsvAsync(Stream csvStream, ImportOptions options);
    
    // Job imports
    Task<ImportPreview<Job>> PreviewJobsFromCsvAsync(Stream csvStream);
    Task<ImportResult> ImportJobsFromCsvAsync(Stream csvStream, ImportOptions options);
    
    // Product imports
    Task<ImportPreview<Product>> PreviewProductsFromCsvAsync(Stream csvStream);
    Task<ImportResult> ImportProductsFromCsvAsync(Stream csvStream, ImportOptions options);
    
    // Asset imports
    Task<ImportPreview<Asset>> PreviewAssetsFromCsvAsync(Stream csvStream);
    Task<ImportResult> ImportAssetsFromCsvAsync(Stream csvStream, ImportOptions options);
    
    // Inventory imports
    Task<ImportPreview<InventoryItem>> PreviewInventoryFromCsvAsync(Stream csvStream);
    Task<ImportResult> ImportInventoryFromCsvAsync(Stream csvStream, ImportOptions options);
    
    // Estimate imports
    Task<ImportPreview<Estimate>> PreviewEstimatesFromCsvAsync(Stream csvStream);
    Task<ImportResult> ImportEstimatesFromCsvAsync(Stream csvStream, ImportOptions options);
    
    // Company imports
    Task<ImportPreview<Company>> PreviewCompaniesFromCsvAsync(Stream csvStream);
    Task<ImportResult> ImportCompaniesFromCsvAsync(Stream csvStream, ImportOptions options);
    
    // Site imports
    Task<ImportPreview<Site>> PreviewSitesFromCsvAsync(Stream csvStream);
    Task<ImportResult> ImportSitesFromCsvAsync(Stream csvStream, ImportOptions options);
    
    // Service Agreement imports
    Task<ImportPreview<ServiceAgreement>> PreviewServiceAgreementsFromCsvAsync(Stream csvStream);
    Task<ImportResult> ImportServiceAgreementsFromCsvAsync(Stream csvStream, ImportOptions options);
    
    // Employee imports
    Task<ImportPreview<Employee>> PreviewEmployeesFromCsvAsync(Stream csvStream);
    Task<ImportResult> ImportEmployeesFromCsvAsync(Stream csvStream, ImportOptions options);
}

/// <summary>
/// Interface for Excel import operations
/// </summary>
public interface IExcelImportService
{
    // Single entity imports
    Task<ImportPreview<Customer>> PreviewCustomersFromExcelAsync(Stream excelStream);
    Task<ImportResult> ImportCustomersFromExcelAsync(Stream excelStream, ImportOptions options);
    
    Task<ImportPreview<Product>> PreviewProductsFromExcelAsync(Stream excelStream);
    Task<ImportResult> ImportProductsFromExcelAsync(Stream excelStream, ImportOptions options);
    
    Task<ImportPreview<Asset>> PreviewAssetsFromExcelAsync(Stream excelStream);
    Task<ImportResult> ImportAssetsFromExcelAsync(Stream excelStream, ImportOptions options);
    
    Task<ImportPreview<InventoryItem>> PreviewInventoryFromExcelAsync(Stream excelStream);
    Task<ImportResult> ImportInventoryFromExcelAsync(Stream excelStream, ImportOptions options);
    
    // Bulk import (Excel with multiple sheets)
    Task<BulkImportPreview> PreviewBulkImportFromExcelAsync(Stream excelStream);
    Task<BulkImportResult> ImportBulkFromExcelAsync(Stream excelStream, BulkImportOptions options);
}

/// <summary>
/// Preview for bulk import operations
/// </summary>
public class BulkImportPreview
{
    public Dictionary<string, int> EntityCounts { get; set; } = new();
    public Dictionary<string, List<ImportError>> ValidationErrors { get; set; } = new();
    public List<string> AvailableSheets { get; set; } = new();
    public bool IsValid => !ValidationErrors.Values.Any(errors => errors.Count > 0);
}

/// <summary>
/// Options for bulk import
/// </summary>
public class BulkImportOptions
{
    public bool UpdateExisting { get; set; } = false;
    public bool StopOnError { get; set; } = false;
    public HashSet<string> EntitiesToImport { get; set; } = new();
}

/// <summary>
/// Result of bulk import operation
/// </summary>
public class BulkImportResult
{
    public bool Success { get; set; }
    public Dictionary<string, ImportResult> Results { get; set; } = new();
    public int TotalImported => Results.Values.Sum(r => r.ImportedCount);
    public int TotalErrors => Results.Values.Sum(r => r.ErrorCount);
}
