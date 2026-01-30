using OneManVan.Shared.Services;

namespace OneManVan.Web.Services;

/// <summary>
/// Web (Blazor Server) implementation of ISettingsStorage.
/// Uses file-based storage in application data directory.
/// For production, consider using IDistributedCache or database storage.
/// </summary>
public class WebSettingsStorage : ISettingsStorage
{
    private readonly string _appDataDirectory;
    private readonly string _settingsPath;
    private readonly Dictionary<string, string> _cache;
    private readonly object _lock = new();

    public WebSettingsStorage(IWebHostEnvironment environment)
    {
        // Use ContentRootPath for server-side storage
        _appDataDirectory = Path.Combine(environment.ContentRootPath, "AppData");
        Directory.CreateDirectory(_appDataDirectory);
        
        _settingsPath = Path.Combine(_appDataDirectory, "web_settings.json");
        _cache = LoadSettings();
    }

    public string AppDataDirectory => _appDataDirectory;

    public string GetString(string key, string defaultValue = "")
    {
        lock (_lock)
        {
            return _cache.TryGetValue(key, out var value) ? value : defaultValue;
        }
    }

    public void SetString(string key, string value)
    {
        lock (_lock)
        {
            _cache[key] = value;
            SaveSettings();
        }
    }

    public bool GetBool(string key, bool defaultValue = false)
    {
        var value = GetString(key, defaultValue.ToString());
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    public void SetBool(string key, bool value)
    {
        SetString(key, value.ToString());
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        var value = GetString(key, defaultValue.ToString());
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    public void SetInt(string key, int value)
    {
        SetString(key, value.ToString());
    }

    public decimal GetDecimal(string key, decimal defaultValue = 0)
    {
        var value = GetString(key, defaultValue.ToString());
        return decimal.TryParse(value, out var result) ? result : defaultValue;
    }

    public void SetDecimal(string key, decimal value)
    {
        SetString(key, value.ToString());
    }

    public void Remove(string key)
    {
        lock (_lock)
        {
            _cache.Remove(key);
            SaveSettings();
        }
    }

    public bool ContainsKey(string key)
    {
        lock (_lock)
        {
            return _cache.ContainsKey(key);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _cache.Clear();
            SaveSettings();
        }
    }

    private Dictionary<string, string> LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json) 
                       ?? new Dictionary<string, string>();
            }
        }
        catch { }
        
        return new Dictionary<string, string>();
    }

    private void SaveSettings()
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_cache, 
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch { }
    }
}
