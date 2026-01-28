using System.IO;
using System.Text.Json;
using OneManVan.Shared.Services;

namespace OneManVan.Services;

/// <summary>
/// Desktop (Windows) implementation of ISettingsStorage.
/// Uses file-based storage in LocalApplicationData folder.
/// </summary>
public class DesktopSettingsStorage : ISettingsStorage
{
    private const string SettingsFileName = "app_settings.json";
    private readonly string _appDataDirectory;
    private readonly string _settingsPath;
    private Dictionary<string, object> _cache;

    public DesktopSettingsStorage()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _appDataDirectory = Path.Combine(localAppData, "OneManVan");
        Directory.CreateDirectory(_appDataDirectory);
        
        _settingsPath = Path.Combine(_appDataDirectory, SettingsFileName);
        _cache = LoadSettings();
    }

    public string AppDataDirectory => _appDataDirectory;

    public string GetString(string key, string defaultValue = "")
    {
        if (_cache.TryGetValue(key, out var value))
        {
            return value?.ToString() ?? defaultValue;
        }
        return defaultValue;
    }

    public void SetString(string key, string value)
    {
        _cache[key] = value;
        SaveSettings();
    }

    public bool GetBool(string key, bool defaultValue = false)
    {
        if (_cache.TryGetValue(key, out var value))
        {
            if (value is bool boolValue) return boolValue;
            if (value is JsonElement je && je.ValueKind == JsonValueKind.True) return true;
            if (value is JsonElement jf && jf.ValueKind == JsonValueKind.False) return false;
            if (bool.TryParse(value?.ToString(), out var parsed)) return parsed;
        }
        return defaultValue;
    }

    public void SetBool(string key, bool value)
    {
        _cache[key] = value;
        SaveSettings();
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        if (_cache.TryGetValue(key, out var value))
        {
            if (value is int intValue) return intValue;
            if (value is JsonElement je && je.TryGetInt32(out var jeInt)) return jeInt;
            if (int.TryParse(value?.ToString(), out var parsed)) return parsed;
        }
        return defaultValue;
    }

    public void SetInt(string key, int value)
    {
        _cache[key] = value;
        SaveSettings();
    }

    public decimal GetDecimal(string key, decimal defaultValue = 0)
    {
        if (_cache.TryGetValue(key, out var value))
        {
            if (value is decimal decValue) return decValue;
            if (value is JsonElement je && je.TryGetDecimal(out var jeDec)) return jeDec;
            if (decimal.TryParse(value?.ToString(), out var parsed)) return parsed;
        }
        return defaultValue;
    }

    public void SetDecimal(string key, decimal value)
    {
        _cache[key] = value;
        SaveSettings();
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        SaveSettings();
    }

    public bool ContainsKey(string key)
    {
        return _cache.ContainsKey(key);
    }

    public void Clear()
    {
        _cache.Clear();
        SaveSettings();
    }

    private Dictionary<string, object> LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<Dictionary<string, object>>(json) 
                       ?? new Dictionary<string, object>();
            }
        }
        catch { }
        
        return new Dictionary<string, object>();
    }

    private void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch { }
    }
}
