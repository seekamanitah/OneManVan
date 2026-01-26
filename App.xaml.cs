using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Services;

namespace OneManVan;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static OneManVanDbContext DbContext { get; private set; } = null!;
    public static IBackupService BackupService { get; private set; } = null!;
    public static ThemeService ThemeService { get; private set; } = null!;
    public static UiScaleService UiScaleService { get; private set; } = null!;
    public static ScheduledBackupService ScheduledBackupService { get; private set; } = null!;
    public static TradeConfigurationService TradeService { get; private set; } = null!;
    public static NotificationBadgeService BadgeService => NotificationBadgeService.Instance;
    public static AppSettings Settings { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Load settings
        Settings = LoadSettings();

        // Initialize trade configuration service
        TradeService = new TradeConfigurationService();

        // Initialize theme service
        ThemeService = new ThemeService();
        ThemeService.Initialize();

        // Initialize UI scale service
        UiScaleService = new UiScaleService();
        UiScaleService.Initialize();

        // Initialize database
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OneManVan",
            Settings.Database.LocalPath);

        // Ensure directory exists
        var dbDir = Path.GetDirectoryName(dbPath)!;
        if (!Directory.Exists(dbDir))
        {
            Directory.CreateDirectory(dbDir);
        }

        var optionsBuilder = new DbContextOptionsBuilder<OneManVanDbContext>();

        if (Settings.Database.Mode == "Local")
        {
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
        else
        {
            // Remote mode - For now, fall back to SQLite
            // SQL Server support requires Microsoft.EntityFrameworkCore.SqlServer NuGet
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        DbContext = new OneManVanDbContext(optionsBuilder.Options);
        
        // Try to ensure schema is up to date
        // If database exists but is missing tables/columns, delete and recreate
        bool needsRecreate = false;
        
        try
        {
            DbContext.Database.EnsureCreated();
            
            // Test that essential tables exist with current schema by attempting queries
            // This will throw if tables/columns are missing
            _ = DbContext.SchemaDefinitions.Any();
            _ = DbContext.Customers.Select(c => c.AccountBalance).FirstOrDefault();
            _ = DbContext.Assets.Select(a => a.AfueRating).FirstOrDefault();
            _ = DbContext.Products.Any();
            _ = DbContext.ProductDocuments.Any();
            _ = DbContext.ServiceAgreements.Any();
        }
        catch (Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException || 
                                   ex.InnerException is Microsoft.Data.Sqlite.SqliteException)
        {
            // Table or column doesn't exist - database schema is outdated
            needsRecreate = true;
        }
        
        if (needsRecreate)
        {
            // Delete the database and recreate with current schema
            DbContext.Dispose();
            
            // Delete the database file
            if (File.Exists(dbPath))
            {
                // Ensure file is not locked
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                try
                {
                    File.Delete(dbPath);
                }
                catch
                {
                    // If delete fails, try with a slight delay
                    Thread.Sleep(100);
                    File.Delete(dbPath);
                }
            }
            
            // Also delete journal/wal files if they exist
            var walPath = dbPath + "-wal";
            var shmPath = dbPath + "-shm";
            if (File.Exists(walPath)) File.Delete(walPath);
            if (File.Exists(shmPath)) File.Delete(shmPath);
            
            // Recreate context and database
            DbContext = new OneManVanDbContext(optionsBuilder.Options);
            DbContext.Database.EnsureCreated();
        }

        BackupService = new BackupService(DbContext, dbPath);

        // Initialize scheduled backup service
        ScheduledBackupService = new ScheduledBackupService(BackupService);
        ScheduledBackupService.Initialize();

        // Initialize notification badge service
        _ = BadgeService.RefreshAllAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Dispose scheduled backup service
        ScheduledBackupService?.Dispose();

        // Auto-backup on exit if enabled
        if (Settings.Backup.AutoBackupOnExit)
        {
            try
            {
            var backupDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "OneManVan",
                    Settings.Backup.BackupFolder);

                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                var backupPath = Path.Combine(backupDir, $"OneManVan_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                BackupService.ExportToJsonAsync(backupPath).Wait();
            }
            catch
            {
                // Silently fail on exit backup
            }
        }

        DbContext?.Dispose();
        base.OnExit(e);
    }

    private static AppSettings LoadSettings()
    {
        var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        if (File.Exists(settingsPath))
        {
            var json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }

        return new AppSettings();
    }
}

/// <summary>
/// Application settings model matching appsettings.json structure.
/// </summary>
public class AppSettings
{
    public DatabaseSettings Database { get; set; } = new();
    public TradeSettings Trade { get; set; } = new();
    public BackupSettings Backup { get; set; } = new();
    public SyncSettings Sync { get; set; } = new();
    public BusinessProfileSettings BusinessProfile { get; set; } = new();
}

public class DatabaseSettings
{
    public string Mode { get; set; } = "Local";
    public string LocalPath { get; set; } = "OneManVan.db";
    public string RemoteConnection { get; set; } = string.Empty;
}

public class TradeSettings
{
    public string Preset { get; set; } = "HVAC";
    public string PresetFile { get; set; } = "Presets/HvacPreset.json";
}

public class BackupSettings
{
    public bool AutoBackupOnExit { get; set; } = true;
    public string BackupFolder { get; set; } = "Backups";
}

public class SyncSettings
{
    public bool Enabled { get; set; } = false;
    public int IntervalMinutes { get; set; } = 5;
}

public class BusinessProfileSettings
{
    public string? CompanyName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
}

