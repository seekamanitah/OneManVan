using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Services;
using OneManVan.Shared.Services;
using OneManVan.Shared.Models;

namespace OneManVan;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static OneManVanDbContext DbContext { get; private set; } = null!;
    public static BackupService BackupService { get; private set; } = null!;
    public static ThemeService ThemeService { get; private set; } = null!;
    public static UiScaleService UiScaleService { get; private set; } = null!;
    public static ScheduledBackupService ScheduledBackupService { get; private set; } = null!;
    public static ISettingsStorage SettingsStorage { get; private set; } = null!;
    public static TradeConfigurationService TradeService { get; private set; } = null!;
    public static NotificationBadgeService BadgeService => NotificationBadgeService.Instance;
    public static AppSettings Settings { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Load settings
        Settings = LoadSettings();

        // Initialize settings storage for shared services
        SettingsStorage = new DesktopSettingsStorage();
        
        // Initialize trade configuration service (now uses shared implementation)
        TradeService = new TradeConfigurationService(SettingsStorage);

        // Initialize theme service
        ThemeService = new ThemeService();
        ThemeService.Initialize();

        // Initialize UI scale service
        UiScaleService = new UiScaleService();
        UiScaleService.Initialize();

        // Initialize dialog service with UI scale support
        DialogService.Initialize(UiScaleService);

        // Initialize database using DatabaseConfigService
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OneManVan");
        
        var dbConfigService = new DatabaseConfigService(configDir);
        var dbConfig = dbConfigService.LoadConfiguration();

        var optionsBuilder = new DbContextOptionsBuilder<OneManVanDbContext>();
        string? dbPath = null; // Track dbPath for SQLite mode

        if (dbConfig.Type == DatabaseType.SQLite)
        {
            // SQLite mode
            dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OneManVan",
                dbConfig.SqliteFilePath);

            // Ensure directory exists
            var dbDir = Path.GetDirectoryName(dbPath)!;
            if (!Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }

            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
        else
        {
            // SQL Server mode (Note: Requires Microsoft.EntityFrameworkCore.SqlServer package)
            var connectionString = dbConfig.GetConnectionString();
            
            // For now, fall back to SQLite if SQL Server package is not available
            // TODO: Install Microsoft.EntityFrameworkCore.SqlServer package for full SQL Server support
            dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OneManVan",
                "OneManVan.db");
            
            var dbDir = Path.GetDirectoryName(dbPath)!;
            if (!Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }
            
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
            
            // Show warning that SQL Server config exists but is not yet supported
            MessageBox.Show(
                "SQL Server configuration detected, but full support requires additional packages.\n\n" +
                "The app will use local SQLite for now. To enable SQL Server:\n" +
                "1. Install Microsoft.EntityFrameworkCore.SqlServer NuGet package\n" +
                "2. Restart the application",
                "SQL Server Support Pending",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
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

        // Initialize SettingsStorage for BackupService
        var settingsStorage = new DesktopSettingsStorage();
        BackupService = new BackupService(DbContext, settingsStorage, dbPath);

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
                var backupPath = Path.Combine(BackupService.GetBackupDirectory(), 
                    $"OneManVan_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                BackupService.CreateBackupAsync(backupPath).Wait();
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

