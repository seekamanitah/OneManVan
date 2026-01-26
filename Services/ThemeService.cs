using System.IO;
using System.Text.Json;
using System.Windows;

namespace OneManVan.Services;

/// <summary>
/// Service for managing application themes (Dark/Light mode).
/// </summary>
public class ThemeService
{
    private const string SettingsFileName = "theme.json";
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OneManVan",
        SettingsFileName);

    public enum AppTheme
    {
        Dark,
        Light
    }

    private AppTheme _currentTheme = AppTheme.Dark;

    public AppTheme CurrentTheme
    {
        get => _currentTheme;
        private set => _currentTheme = value;
    }

    public bool IsDarkMode => CurrentTheme == AppTheme.Dark;

    /// <summary>
    /// Initializes the theme service and loads saved preference.
    /// </summary>
    public void Initialize()
    {
        LoadThemePreference();
        ApplyTheme(CurrentTheme);
    }

    /// <summary>
    /// Sets the application theme.
    /// </summary>
    public void SetTheme(AppTheme theme)
    {
        CurrentTheme = theme;
        ApplyTheme(theme);
        SaveThemePreference();
    }

    /// <summary>
    /// Toggles between dark and light themes.
    /// </summary>
    public void ToggleTheme()
    {
        SetTheme(CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
    }

    /// <summary>
    /// Applies the specified theme to the application.
    /// </summary>
    private void ApplyTheme(AppTheme theme)
    {
        var app = Application.Current;
        if (app == null) return;

        // Clear existing theme dictionaries
        var toRemove = app.Resources.MergedDictionaries
            .Where(d => d.Source?.OriginalString.Contains("Theme") == true)
            .ToList();

        foreach (var dict in toRemove)
        {
            app.Resources.MergedDictionaries.Remove(dict);
        }

        // Add the new theme dictionary
        var themePath = theme == AppTheme.Dark
            ? "Themes/DarkTheme.xaml"
            : "Themes/LightTheme.xaml";

        var themeDict = new ResourceDictionary
        {
            Source = new Uri(themePath, UriKind.Relative)
        };

        app.Resources.MergedDictionaries.Add(themeDict);
    }

    /// <summary>
    /// Loads the theme preference from settings file.
    /// </summary>
    private void LoadThemePreference()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<ThemeSettings>(json);
                if (settings != null)
                {
                    CurrentTheme = settings.Theme;
                }
            }
        }
        catch
        {
            // Default to dark theme on error
            CurrentTheme = AppTheme.Dark;
        }
    }

    /// <summary>
    /// Saves the theme preference to settings file.
    /// </summary>
    private void SaveThemePreference()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var settings = new ThemeSettings { Theme = CurrentTheme };
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Silently fail on save errors
        }
    }

    private class ThemeSettings
    {
        public AppTheme Theme { get; set; } = AppTheme.Dark;
    }
}
