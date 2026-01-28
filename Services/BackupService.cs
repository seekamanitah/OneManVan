using OneManVan.Shared.Data;
using OneManVan.Shared.Services;

namespace OneManVan.Services;

/// <summary>
/// Desktop implementation of backup/restore functionality.
/// Inherits common logic from BackupServiceBase in OneManVan.Shared.
/// </summary>
public class BackupService : BackupServiceBase
{
    private readonly string _dbPath;

    public BackupService(OneManVanDbContext context, ISettingsStorage settings, string dbPath) 
        : base(context, settings)
    {
        _dbPath = dbPath;
    }

    /// <summary>
    /// Gets the SQLite database file path (Desktop-specific).
    /// </summary>
    public string GetDatabasePath() => _dbPath;

    /// <summary>
    /// Exports data to a JSON file (Desktop convenience method).
    /// Maps to CreateBackupAsync from base class.
    /// </summary>
    public async Task<BackupResult> ExportToJsonAsync(string filePath)
    {
        return await CreateBackupAsync(filePath);
    }

    /// <summary>
    /// Exports data to a ZIP file (Desktop convenience method).
    /// Maps to CreateZipBackupAsync from base class.
    /// </summary>
    public async Task<BackupResult> ExportToZipAsync(string filePath)
    {
        return await CreateZipBackupAsync(filePath);
    }

    /// <summary>
    /// Imports data from a JSON/ZIP file (Desktop convenience method).
    /// Maps to RestoreBackupAsync from base class.
    /// </summary>
    public async Task<BackupResult> ImportFromJsonAsync(string filePath)
    {
        return await RestoreBackupAsync(filePath, mergeMode: true);
    }

    // All backup/restore methods inherited from BackupServiceBase:
    // - CreateBackupAsync()
    // - CreateZipBackupAsync()
    // - RestoreBackupAsync()
    // - GetAvailableBackupsAsync()
    // - DeleteBackupAsync()
    // - ExportEntityAsync<T>()
    // - GetBackupDirectory()
}
