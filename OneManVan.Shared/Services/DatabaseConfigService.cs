using System.Text.Json;
using OneManVan.Shared.Models;

namespace OneManVan.Shared.Services;

/// <summary>
/// Service for managing database configuration.
/// Handles loading, saving, and validating database connection settings.
/// </summary>
public class DatabaseConfigService
{
    private readonly string _configFilePath;
    private DatabaseConfig? _currentConfig;

    public DatabaseConfigService(string configDirectory)
    {
        _configFilePath = Path.Combine(configDirectory, "database-config.json");
    }

    /// <summary>
    /// Loads the database configuration from file or creates default
    /// </summary>
    public DatabaseConfig LoadConfiguration()
    {
        if (_currentConfig != null)
            return _currentConfig;

        if (File.Exists(_configFilePath))
        {
            try
            {
                var json = File.ReadAllText(_configFilePath);
                _currentConfig = JsonSerializer.Deserialize<DatabaseConfig>(json);
                
                if (_currentConfig != null)
                    return _currentConfig;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load database config: {ex.Message}");
            }
        }

        // Create default configuration
        _currentConfig = DatabaseConfig.CreateDefault();
        return _currentConfig;
    }

    /// <summary>
    /// Saves the database configuration to file
    /// </summary>
    public void SaveConfiguration(DatabaseConfig config)
    {
        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Serialize and save
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_configFilePath, json);
            _currentConfig = config;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save database configuration: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the current configuration
    /// </summary>
    public DatabaseConfig GetCurrentConfiguration()
    {
        return _currentConfig ?? LoadConfiguration();
    }

    /// <summary>
    /// Resets configuration to default
    /// </summary>
    public void ResetToDefault()
    {
        _currentConfig = DatabaseConfig.CreateDefault();
        SaveConfiguration(_currentConfig);
    }

    /// <summary>
    /// Checks if configuration has changed since last load
    /// </summary>
    public bool HasConfigurationChanged(DatabaseConfig newConfig)
    {
        var current = GetCurrentConfiguration();
        
        if (current.Type != newConfig.Type)
            return true;

        if (current.Type == DatabaseType.SQLite)
        {
            return current.SqliteFilePath != newConfig.SqliteFilePath;
        }
        else
        {
            return current.ServerAddress != newConfig.ServerAddress ||
                   current.ServerPort != newConfig.ServerPort ||
                   current.DatabaseName != newConfig.DatabaseName ||
                   current.Username != newConfig.Username ||
                   current.Password != newConfig.Password ||
                   current.TrustServerCertificate != newConfig.TrustServerCertificate ||
                   current.Encrypt != newConfig.Encrypt;
        }
    }

    /// <summary>
    /// Exports configuration to JSON string
    /// </summary>
    public string ExportConfiguration(DatabaseConfig config)
    {
        // Create a copy without password for export
        var exportConfig = new DatabaseConfig
        {
            Type = config.Type,
            SqliteFilePath = config.SqliteFilePath,
            ServerAddress = config.ServerAddress,
            ServerPort = config.ServerPort,
            DatabaseName = config.DatabaseName,
            Username = config.Username,
            Password = "********", // Masked
            TrustServerCertificate = config.TrustServerCertificate,
            Encrypt = config.Encrypt,
            ConnectionTimeout = config.ConnectionTimeout
        };

        return JsonSerializer.Serialize(exportConfig, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
