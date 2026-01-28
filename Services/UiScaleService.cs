using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace OneManVan.Services;

/// <summary>
/// Service for managing UI scale factor for accessibility.
/// Allows users to adjust the overall UI size for better readability.
/// </summary>
public class UiScaleService
{
    private const string SettingsFileName = "uiscale.json";
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OneManVan",
        SettingsFileName);

    public const double MinScale = 0.8;
    public const double MaxScale = 1.5;
    public const double DefaultScale = 1.0;
    public const double ScaleStep = 0.05;

    private double _currentScale = DefaultScale;

    public double CurrentScale
    {
        get => _currentScale;
        private set => _currentScale = Math.Clamp(value, MinScale, MaxScale);
    }

    /// <summary>
    /// Event fired when the UI scale changes.
    /// </summary>
    public event EventHandler<double>? ScaleChanged;

    /// <summary>
    /// Initializes the service and loads saved scale preference.
    /// </summary>
    public void Initialize()
    {
        LoadScalePreference();
        ApplyScale(CurrentScale);
    }

    /// <summary>
    /// Sets the UI scale factor.
    /// </summary>
    /// <param name="scale">Scale factor between 0.8 and 1.5</param>
    public void SetScale(double scale)
    {
        CurrentScale = scale;
        ApplyScale(CurrentScale);
        SaveScalePreference();
        ScaleChanged?.Invoke(this, CurrentScale);
    }

    /// <summary>
    /// Increases the scale by one step.
    /// </summary>
    public void IncreaseScale()
    {
        SetScale(CurrentScale + ScaleStep);
    }

    /// <summary>
    /// Decreases the scale by one step.
    /// </summary>
    public void DecreaseScale()
    {
        SetScale(CurrentScale - ScaleStep);
    }

    /// <summary>
    /// Resets the scale to default.
    /// </summary>
    public void ResetScale()
    {
        SetScale(DefaultScale);
    }

    /// <summary>
    /// Gets the scale as a percentage string (e.g., "100%").
    /// </summary>
    public string GetScalePercentage()
    {
        return $"{CurrentScale * 100:N0}%";
    }

    /// <summary>
    /// Adjusts a dialog window's size to account for UI scaling.
    /// Call this method after ShowDialog to ensure content fits properly at all scale levels.
    /// </summary>
    /// <param name="dialog">The dialog window to adjust</param>
    /// <param name="baseHeight">Optional base height. If not provided, uses current height.</param>
    /// <param name="baseWidth">Optional base width. If not provided, uses current width.</param>
    public void AdjustDialogForScale(Window dialog, double? baseHeight = null, double? baseWidth = null)
    {
        if (dialog == null) return;

        var targetHeight = baseHeight ?? dialog.Height;
        var targetWidth = baseWidth ?? dialog.Width;

        // If scale is greater than 1.0, increase dialog size proportionally
        if (CurrentScale > 1.0)
        {
            var scaleFactor = Math.Min(CurrentScale, 1.3); // Cap at 1.3 to avoid huge dialogs
            
            if (!double.IsNaN(targetHeight))
            {
                dialog.Height = targetHeight * scaleFactor;
            }
            
            if (!double.IsNaN(targetWidth))
            {
                dialog.Width = targetWidth * scaleFactor;
            }

            // If dialog has min/max constraints, adjust those too
            if (dialog.MinHeight > 0)
            {
                dialog.MinHeight *= scaleFactor;
            }
            if (dialog.MaxHeight > 0 && dialog.MaxHeight < double.PositiveInfinity)
            {
                dialog.MaxHeight *= scaleFactor;
            }
        }
    }

    /// <summary>
    /// Makes a dialog resizable if UI scale is above threshold.
    /// Useful for fixed-size dialogs that might overflow at high scales.
    /// </summary>
    /// <param name="dialog">The dialog window to adjust</param>
    /// <param name="scaleThreshold">Scale threshold above which to make resizable (default: 1.15)</param>
    public void MakeDialogScaleAware(Window dialog, double scaleThreshold = 1.15)
    {
        if (dialog == null) return;

        if (CurrentScale >= scaleThreshold && dialog.ResizeMode == ResizeMode.NoResize)
        {
            dialog.ResizeMode = ResizeMode.CanResize;
        }
    }

    /// <summary>
    /// Applies the scale transform to the main window.
    /// </summary>
    private void ApplyScale(double scale)
    {
        var app = Application.Current;
        if (app?.MainWindow == null) return;

        // Find the main content area to scale
        // We'll apply scale via a resource that pages can use
        app.Resources["UiScaleFactor"] = scale;
        app.Resources["UiScaleTransform"] = new ScaleTransform(scale, scale);

        // Update font size resources based on scale
        UpdateScaledFontSizes(scale);
    }

    /// <summary>
    /// Updates font size resources based on scale factor.
    /// </summary>
    private void UpdateScaledFontSizes(double scale)
    {
        var app = Application.Current;
        if (app == null) return;

        // Base font sizes that will be scaled
        app.Resources["ScaledFontSizeSmall"] = 11.0 * scale;
        app.Resources["ScaledFontSizeNormal"] = 13.0 * scale;
        app.Resources["ScaledFontSizeMedium"] = 14.0 * scale;
        app.Resources["ScaledFontSizeLarge"] = 16.0 * scale;
        app.Resources["ScaledFontSizeXLarge"] = 18.0 * scale;
        app.Resources["ScaledFontSizeTitle"] = 24.0 * scale;
        app.Resources["ScaledFontSizeHeader"] = 28.0 * scale;
        app.Resources["ScaledFontSizeHero"] = 36.0 * scale;
    }

    /// <summary>
    /// Loads scale preference from settings file.
    /// </summary>
    private void LoadScalePreference()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<ScaleSettings>(json);
                if (settings != null)
                {
                    CurrentScale = settings.Scale;
                }
            }
        }
        catch
        {
            CurrentScale = DefaultScale;
        }
    }

    /// <summary>
    /// Saves scale preference to settings file.
    /// </summary>
    private void SaveScalePreference()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var settings = new ScaleSettings { Scale = CurrentScale };
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Silently fail on save errors
        }
    }

    private class ScaleSettings
    {
        public double Scale { get; set; } = DefaultScale;
    }
}
