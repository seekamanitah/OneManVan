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

        // HTTP Client for API calls (if needed)
        builder.Services.AddHttpClient("OneManVanAPI", client =>
        {
            // Configure for your API endpoint
            client.BaseAddress = new Uri("https://your-api-url.com");
        });

        return builder.Build();
    }
}

// Mobile Settings Storage Implementation
public class MobileSettingsStorage : ISettingsStorage
{
    public Task<string?> GetValueAsync(string key)
    {
        var value = Preferences.Get(key, null);
        return Task.FromResult(value);
    }

    public Task SetValueAsync(string key, string value)
    {
        Preferences.Set(key, value);
        return Task.CompletedTask;
    }

    public Task RemoveValueAsync(string key)
    {
        Preferences.Remove(key);
        return Task.CompletedTask;
    }
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
            _logger.LogInformation($"?? Database path: {dbPath}");
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory))
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _logger.LogInformation($"?? Created directory: {directory}");
                }
                else
                {
                    _logger.LogInformation($"? Directory exists: {directory}");
                }
            }
            
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();
            
            _logger.LogInformation("? Database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Failed to initialize database");
            throw;
        }
    }
}
