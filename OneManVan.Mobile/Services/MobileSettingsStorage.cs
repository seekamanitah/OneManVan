using OneManVan.Shared.Services;

namespace OneManVan.Mobile.Services;

/// <summary>
/// Mobile (MAUI) implementation of ISettingsStorage.
/// Uses MAUI Preferences API for cross-platform settings storage.
/// </summary>
public class MobileSettingsStorage : ISettingsStorage
{
    public string AppDataDirectory => FileSystem.AppDataDirectory;

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
        // MAUI Preferences doesn't directly support decimal, so use string
        var stringValue = Preferences.Get(key, defaultValue.ToString());
        if (decimal.TryParse(stringValue, out var result))
        {
            return result;
        }
        return defaultValue;
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
}
