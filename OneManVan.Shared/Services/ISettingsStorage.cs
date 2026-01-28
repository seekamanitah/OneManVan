namespace OneManVan.Shared.Services;

/// <summary>
/// Platform-agnostic interface for storing application settings.
/// Each platform (Desktop, Mobile, Web) implements this interface
/// using the appropriate storage mechanism.
/// </summary>
public interface ISettingsStorage
{
    /// <summary>
    /// Gets a string value from settings.
    /// </summary>
    string GetString(string key, string defaultValue = "");

    /// <summary>
    /// Sets a string value in settings.
    /// </summary>
    void SetString(string key, string value);

    /// <summary>
    /// Gets a boolean value from settings.
    /// </summary>
    bool GetBool(string key, bool defaultValue = false);

    /// <summary>
    /// Sets a boolean value in settings.
    /// </summary>
    void SetBool(string key, bool value);

    /// <summary>
    /// Gets an integer value from settings.
    /// </summary>
    int GetInt(string key, int defaultValue = 0);

    /// <summary>
    /// Sets an integer value in settings.
    /// </summary>
    void SetInt(string key, int value);

    /// <summary>
    /// Gets a decimal value from settings.
    /// </summary>
    decimal GetDecimal(string key, decimal defaultValue = 0);

    /// <summary>
    /// Sets a decimal value in settings.
    /// </summary>
    void SetDecimal(string key, decimal value);

    /// <summary>
    /// Removes a key from settings.
    /// </summary>
    void Remove(string key);

    /// <summary>
    /// Checks if a key exists in settings.
    /// </summary>
    bool ContainsKey(string key);

    /// <summary>
    /// Clears all settings.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets the application data directory for file storage.
    /// </summary>
    string AppDataDirectory { get; }
}
