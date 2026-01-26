using System.IO;
using System.Text.Json;
using System.Timers;

namespace OneManVan.Services;

/// <summary>
/// Service for managing scheduled automatic backups.
/// </summary>
public class ScheduledBackupService : IDisposable
{
    private const string SettingsFileName = "backup_schedule.json";
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OneManVan",
        SettingsFileName);

    private readonly IBackupService _backupService;
    private System.Timers.Timer? _timer;
    private BackupScheduleSettings _settings;

    public event EventHandler<BackupCompletedEventArgs>? BackupCompleted;

    public BackupScheduleSettings Settings => _settings;

    public ScheduledBackupService(IBackupService backupService)
    {
        _backupService = backupService;
        _settings = LoadSettings();
    }

    /// <summary>
    /// Initializes the scheduled backup service.
    /// </summary>
    public void Initialize()
    {
        if (_settings.Enabled)
        {
            StartScheduler();
        }

        // Check if a backup is due on startup
        CheckAndRunBackupAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Updates the backup schedule settings.
    /// </summary>
    public void UpdateSettings(BackupScheduleSettings settings)
    {
        _settings = settings;
        SaveSettings();

        // Restart scheduler with new settings
        StopScheduler();
        if (_settings.Enabled)
        {
            StartScheduler();
        }
    }

    /// <summary>
    /// Enables or disables scheduled backups.
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        _settings.Enabled = enabled;
        SaveSettings();

        if (enabled)
        {
            StartScheduler();
        }
        else
        {
            StopScheduler();
        }
    }

    /// <summary>
    /// Runs a backup immediately.
    /// </summary>
    public async Task<BackupResult> RunBackupNowAsync()
    {
        return await PerformBackupAsync();
    }

    /// <summary>
    /// Gets the path to the backup folder.
    /// </summary>
    public string GetBackupFolder()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OneManVan",
            _settings.BackupFolder);

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        return folder;
    }

    /// <summary>
    /// Gets a list of existing backups.
    /// </summary>
    public IEnumerable<BackupFileInfo> GetExistingBackups()
    {
        var folder = GetBackupFolder();
        if (!Directory.Exists(folder))
        {
            return [];
        }

        return Directory.GetFiles(folder, "*.zip")
            .Concat(Directory.GetFiles(folder, "*.json"))
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTime)
            .Select(f => new BackupFileInfo
            {
                FileName = f.Name,
                FilePath = f.FullName,
                CreatedAt = f.CreationTime,
                SizeBytes = f.Length,
                IsCompressed = f.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)
            });
    }

    /// <summary>
    /// Cleans up old backups based on retention settings.
    /// </summary>
    public int CleanupOldBackups()
    {
        var backups = GetExistingBackups().ToList();
        var toDelete = new List<string>();

        // Keep only the specified number of backups
        if (_settings.MaxBackupsToKeep > 0 && backups.Count > _settings.MaxBackupsToKeep)
        {
            toDelete.AddRange(backups
                .Skip(_settings.MaxBackupsToKeep)
                .Select(b => b.FilePath));
        }

        // Delete backups older than retention period
        if (_settings.RetentionDays > 0)
        {
            var cutoff = DateTime.Now.AddDays(-_settings.RetentionDays);
            toDelete.AddRange(backups
                .Where(b => b.CreatedAt < cutoff)
                .Select(b => b.FilePath));
        }

        var deleted = 0;
        foreach (var path in toDelete.Distinct())
        {
            try
            {
                File.Delete(path);
                deleted++;
            }
            catch
            {
                // Ignore deletion errors
            }
        }

        return deleted;
    }

    private void StartScheduler()
    {
        StopScheduler();

        // Check every hour if a backup is due
        _timer = new System.Timers.Timer(TimeSpan.FromHours(1).TotalMilliseconds);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
        _timer.Start();
    }

    private void StopScheduler()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }

    private async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        await CheckAndRunBackupAsync();
    }

    private async Task CheckAndRunBackupAsync()
    {
        if (!_settings.Enabled)
            return;

        var now = DateTime.Now;
        var lastBackup = _settings.LastBackupTime;

        var shouldBackup = _settings.Frequency switch
        {
            BackupFrequency.Daily => lastBackup.Date < now.Date,
            BackupFrequency.Weekly => (now - lastBackup).TotalDays >= 7,
            BackupFrequency.Monthly => lastBackup.Month != now.Month || lastBackup.Year != now.Year,
            _ => false
        };

        // Check if we're in the preferred time window (within 2 hours of preferred time)
        if (shouldBackup && _settings.PreferredHour >= 0)
        {
            var hourDiff = Math.Abs(now.Hour - _settings.PreferredHour);
            if (hourDiff > 2 && hourDiff < 22) // Allow 2-hour window
            {
                return; // Wait for preferred time
            }
        }

        if (shouldBackup)
        {
            await PerformBackupAsync();
        }
    }

    private async Task<BackupResult> PerformBackupAsync()
    {
        var result = new BackupResult();

        try
        {
            var folder = GetBackupFolder();
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var extension = _settings.UseCompression ? "zip" : "json";
            var fileName = $"OneManVan_Backup_{timestamp}.{extension}";
            var filePath = Path.Combine(folder, fileName);

            if (_settings.UseCompression)
            {
                await _backupService.ExportToZipAsync(filePath);
            }
            else
            {
                await _backupService.ExportToJsonAsync(filePath);
            }

            // Update last backup time
            _settings.LastBackupTime = DateTime.Now;
            SaveSettings();

            // Cleanup old backups
            var cleaned = CleanupOldBackups();

            result.Success = true;
            result.FilePath = filePath;
            result.CleanedUpCount = cleaned;

            BackupCompleted?.Invoke(this, new BackupCompletedEventArgs(result));
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private BackupScheduleSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<BackupScheduleSettings>(json) ?? new BackupScheduleSettings();
            }
        }
        catch
        {
            // Return defaults on error
        }

        return new BackupScheduleSettings();
    }

    private void SaveSettings()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Silently fail
        }
    }

    public void Dispose()
    {
        StopScheduler();
    }
}

/// <summary>
/// Settings for scheduled backups.
/// </summary>
public class BackupScheduleSettings
{
    public bool Enabled { get; set; } = false;
    public BackupFrequency Frequency { get; set; } = BackupFrequency.Daily;
    public int PreferredHour { get; set; } = 2; // 2 AM
    public bool UseCompression { get; set; } = true;
    public string BackupFolder { get; set; } = "Backups";
    public int MaxBackupsToKeep { get; set; } = 10;
    public int RetentionDays { get; set; } = 30;
    public DateTime LastBackupTime { get; set; } = DateTime.MinValue;
}

/// <summary>
/// Backup frequency options.
/// </summary>
public enum BackupFrequency
{
    Daily,
    Weekly,
    Monthly
}

/// <summary>
/// Result of a backup operation.
/// </summary>
public class BackupResult
{
    public bool Success { get; set; }
    public string? FilePath { get; set; }
    public string? ErrorMessage { get; set; }
    public int CleanedUpCount { get; set; }
}

/// <summary>
/// Information about a backup file.
/// </summary>
public class BackupFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long SizeBytes { get; set; }
    public bool IsCompressed { get; set; }

    public string SizeDisplay => SizeBytes switch
    {
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:N1} KB",
        _ => $"{SizeBytes / (1024.0 * 1024.0):N1} MB"
    };
}

/// <summary>
/// Event args for backup completed event.
/// </summary>
public class BackupCompletedEventArgs : EventArgs
{
    public BackupResult Result { get; }

    public BackupCompletedEventArgs(BackupResult result)
    {
        Result = result;
    }
}
