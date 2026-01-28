using OneManVan.Shared.Data;
using OneManVan.Shared.Services;

namespace OneManVan.Mobile.Services;

/// <summary>
/// Mobile implementation of backup/restore functionality.
/// Inherits common logic from BackupServiceBase in OneManVan.Shared.
/// </summary>
public class MobileBackupService : BackupServiceBase
{
    public MobileBackupService(OneManVanDbContext context, ISettingsStorage settings) 
        : base(context, settings)
    {
    }

    /// <summary>
    /// Creates a backup and shares it via the platform's share dialog (Mobile-specific).
    /// </summary>
    public async Task<BackupResult> CreateAndShareBackupAsync()
    {
        var result = await CreateBackupAsync();
        if (result.Success && result.FilePath != null)
        {
            await ShareBackupAsync(result.FilePath);
        }
        return result;
    }

    /// <summary>
    /// Shares a backup file via the platform's share dialog.
    /// </summary>
    private async Task ShareBackupAsync(string filePath)
    {
        try
        {
            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "Share OneManVan Backup",
                File = new ShareFile(filePath)
            });
        }
        catch
        {
            // Silently fail if sharing not supported
        }
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
