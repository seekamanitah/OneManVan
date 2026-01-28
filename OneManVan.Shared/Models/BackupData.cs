using OneManVan.Shared.Models;

namespace OneManVan.Shared.Services;

/// <summary>
/// Container for all backup data across platforms.
/// Used for JSON serialization of database exports.
/// </summary>
public class BackupData
{
    /// <summary>
    /// Timestamp when backup was created (UTC).
    /// Maps to both Desktop.ExportedAt and Mobile.BackupDate.
    /// </summary>
    public DateTime BackupDate { get; set; }
    
    /// <summary>
    /// Application version that created the backup.
    /// </summary>
    public string AppVersion { get; set; } = "3.0";
    
    // Core entities
    public List<Customer> Customers { get; set; } = [];
    public List<Site> Sites { get; set; } = [];
    public List<Asset> Assets { get; set; } = [];
    public List<CustomField> CustomFields { get; set; } = [];
    public List<SchemaDefinition> SchemaDefinitions { get; set; } = [];
    
    // Business entities
    public List<Estimate> Estimates { get; set; } = [];
    public List<EstimateLine> EstimateLines { get; set; } = [];
    public List<Job> Jobs { get; set; } = [];
    public List<TimeEntry> TimeEntries { get; set; } = [];
    public List<Invoice> Invoices { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
    
    // Inventory
    public List<InventoryItem> InventoryItems { get; set; } = [];
    public List<InventoryLog> InventoryLogs { get; set; } = [];
    
    /// <summary>
    /// Gets total count of all entities in backup.
    /// </summary>
    public int TotalRecordCount =>
        Customers.Count + Sites.Count + Assets.Count + CustomFields.Count +
        Estimates.Count + EstimateLines.Count + Jobs.Count + TimeEntries.Count +
        Invoices.Count + Payments.Count + InventoryItems.Count + InventoryLogs.Count +
        SchemaDefinitions.Count;
}
