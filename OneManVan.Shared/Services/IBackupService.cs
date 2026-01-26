namespace OneManVan.Shared.Services;

/// <summary>
/// Interface for backup/restore operations.
/// Platform-specific implementations will handle file system access.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Creates a full backup of all data as JSON.
    /// </summary>
    Task<BackupResult> CreateBackupAsync(string? customPath = null);

    /// <summary>
    /// Restores data from a backup file.
    /// </summary>
    Task<BackupResult> RestoreBackupAsync(string backupPath);

    /// <summary>
    /// Gets the list of available backups.
    /// </summary>
    Task<IEnumerable<BackupInfo>> GetAvailableBackupsAsync();

    /// <summary>
    /// Deletes a backup file.
    /// </summary>
    Task<bool> DeleteBackupAsync(string backupPath);

    /// <summary>
    /// Exports a single entity as JSON.
    /// </summary>
    Task<string> ExportEntityAsync<T>(T entity) where T : class;

    /// <summary>
    /// Gets the default backup directory path.
    /// </summary>
    string GetBackupDirectory();
}

/// <summary>
/// Result of a backup operation.
/// </summary>
public class BackupResult
{
    public bool Success { get; set; }
    public string? FilePath { get; set; }
    public string? Message { get; set; }
    public int RecordCount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static BackupResult Succeeded(string filePath, int recordCount, string? message = null)
        => new() { Success = true, FilePath = filePath, RecordCount = recordCount, Message = message };

    public static BackupResult Failed(string message)
        => new() { Success = false, Message = message };
}

/// <summary>
/// Information about an available backup.
/// </summary>
public class BackupInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long FileSizeBytes { get; set; }
    public string FileSizeDisplay => FileSizeBytes switch
    {
        < 1024 => $"{FileSizeBytes} B",
        < 1024 * 1024 => $"{FileSizeBytes / 1024.0:F1} KB",
        _ => $"{FileSizeBytes / (1024.0 * 1024.0):F1} MB"
    };
}
