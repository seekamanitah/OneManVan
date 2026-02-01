using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneManVan.Shared.Data;
using OneManVan.Shared.Services;

namespace OneManVan.MauiBlazor;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Add Blazor WebView
        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // Configure Database
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "onemanvan.db");
        
        // CRITICAL: Ensure directory exists before database operations
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        builder.Services.AddDbContextFactory<OneManVanDbContext>(options =>
        {
            options.UseSqlite($"Data Source={dbPath}");
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        });

        // Register Services
        builder.Services.AddSingleton<ISettingsStorage, MobileSettingsStorage>();
        builder.Services.AddSingleton<TradeConfigurationService>();
        builder.Services.AddScoped<DatabaseInitializer>();

        return builder.Build();
    }
}

// Mobile Settings Storage Implementation
public class MobileSettingsStorage : ISettingsStorage
{
    public string GetString(string key, string defaultValue = "")
    {
        return Preferences.Get(key, defaultValue);
    }

    public void SetString(string key, string value)
    {
        Preferences.Set(key, value);
    }

    public bool GetBool(string key, bool defaultValue = false)
    {
        return Preferences.Get(key, defaultValue);
    }

    public void SetBool(string key, bool value)
    {
        Preferences.Set(key, value);
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        return Preferences.Get(key, defaultValue);
    }

    public void SetInt(string key, int value)
    {
        Preferences.Set(key, value);
    }

    public decimal GetDecimal(string key, decimal defaultValue = 0)
    {
        var stringValue = Preferences.Get(key, defaultValue.ToString());
        return decimal.TryParse(stringValue, out var result) ? result : defaultValue;
    }

    public void SetDecimal(string key, decimal value)
    {
        Preferences.Set(key, value.ToString());
    }

    public void Remove(string key)
    {
        Preferences.Remove(key);
    }

    public bool ContainsKey(string key)
    {
        return Preferences.ContainsKey(key);
    }

    public void Clear()
    {
        Preferences.Clear();
    }

    public string AppDataDirectory => FileSystem.AppDataDirectory;
}

// Database Initializer
public class DatabaseInitializer
{
    private readonly IDbContextFactory<OneManVanDbContext> _contextFactory;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        IDbContextFactory<OneManVanDbContext> contextFactory,
        ILogger<DatabaseInitializer> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            // Get database path for logging
            var dbPath = context.Database.GetDbConnection().DataSource;
            _logger.LogInformation("Database path: {DbPath}", dbPath);
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory))
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _logger.LogInformation("Created directory: {Directory}", directory);
                }
                else
                {
                    _logger.LogInformation("Directory exists: {Directory}", directory);
                }
            }
            
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();
            
            _logger.LogInformation("Database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database");
            throw;
        }
    }
}
