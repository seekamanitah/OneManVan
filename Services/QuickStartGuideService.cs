using System.IO;
using System.Text.Json;

namespace OneManVan.Services;

/// <summary>
/// Service for managing first-time user experience and tips.
/// </summary>
public class QuickStartGuideService
{
    private readonly string _settingsPath;
    private QuickStartSettings _settings;

    public QuickStartGuideService()
    {
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OneManVan");
        
        Directory.CreateDirectory(appDataDir);
        _settingsPath = Path.Combine(appDataDir, "quickstart.json");
        _settings = LoadSettings();
    }

    /// <summary>
    /// Gets whether this is the first launch of the application.
    /// </summary>
    public bool IsFirstLaunch => !_settings.HasCompletedOnboarding;

    /// <summary>
    /// Gets whether the user has dismissed the welcome banner.
    /// </summary>
    public bool HasDismissedWelcome => _settings.HasDismissedWelcome;

    /// <summary>
    /// Gets the list of completed tutorial steps.
    /// </summary>
    public List<string> CompletedSteps => _settings.CompletedSteps;

    /// <summary>
    /// Marks onboarding as complete.
    /// </summary>
    public void CompleteOnboarding()
    {
        _settings.HasCompletedOnboarding = true;
        _settings.OnboardingCompletedAt = DateTime.UtcNow;
        SaveSettings();
    }

    /// <summary>
    /// Dismisses the welcome banner.
    /// </summary>
    public void DismissWelcome()
    {
        _settings.HasDismissedWelcome = true;
        SaveSettings();
    }

    /// <summary>
    /// Marks a tutorial step as complete.
    /// </summary>
    public void CompleteStep(string stepId)
    {
        if (!_settings.CompletedSteps.Contains(stepId))
        {
            _settings.CompletedSteps.Add(stepId);
            SaveSettings();
        }
    }

    /// <summary>
    /// Checks if a specific step is complete.
    /// </summary>
    public bool IsStepComplete(string stepId) => _settings.CompletedSteps.Contains(stepId);

    /// <summary>
    /// Gets the next recommended step for the user.
    /// </summary>
    public QuickStartStep? GetNextStep()
    {
        foreach (var step in AllSteps)
        {
            if (!_settings.CompletedSteps.Contains(step.Id))
                return step;
        }
        return null;
    }

    /// <summary>
    /// Gets all quick start steps.
    /// </summary>
    public static List<QuickStartStep> AllSteps =>
    [
        new QuickStartStep
        {
            Id = "add_customer",
            Title = "Add Your First Customer",
            Description = "Create a customer profile to start tracking their HVAC equipment and service history.",
            Icon = "",
            NavigateTo = "Customers"
        },
        new QuickStartStep
        {
            Id = "add_asset",
            Title = "Log an HVAC Asset",
            Description = "Track an air conditioner, furnace, or other equipment with warranty information.",
            Icon = "",
            NavigateTo = "Assets"
        },
        new QuickStartStep
        {
            Id = "create_estimate",
            Title = "Create an Estimate",
            Description = "Build a job proposal with labor, parts, and pricing for a customer.",
            Icon = "",
            NavigateTo = "Estimates"
        },
        new QuickStartStep
        {
            Id = "add_inventory",
            Title = "Add Inventory Items",
            Description = "Track your stock of filters, parts, and materials.",
            Icon = "",
            NavigateTo = "Inventory"
        },
        new QuickStartStep
        {
            Id = "export_backup",
            Title = "Create Your First Backup",
            Description = "Export your data to keep it safe. Backups can be restored anytime.",
            Icon = "",
            NavigateTo = "Settings"
        }
    ];

    /// <summary>
    /// Gets HVAC tips for display.
    /// </summary>
    public static List<HvacTip> HvacTips =>
    [
        new HvacTip
        {
            Category = "Maintenance",
            Title = "Annual Tune-Up Timing",
            Content = "Schedule furnace tune-ups in fall and AC tune-ups in spring to catch issues before peak season.",
            Icon = ""
        },
        new HvacTip
        {
            Category = "Refrigerant",
            Title = "R-22 Phase Out",
            Content = "R-22 (Freon) is no longer manufactured. Recommend R-410A system upgrades to customers with older units.",
            Icon = ""
        },
        new HvacTip
        {
            Category = "Efficiency",
            Title = "SEER Ratings",
            Content = "Systems with SEER 16+ often qualify for utility rebates. Check local programs for your customers.",
            Icon = ""
        },
        new HvacTip
        {
            Category = "Safety",
            Title = "Gas Line Inspection",
            Content = "Always perform a gas leak check on natural gas and propane systems during annual maintenance.",
            Icon = ""
        },
        new HvacTip
        {
            Category = "Warranty",
            Title = "Documentation Matters",
            Content = "Keep install dates and serial numbers documented - manufacturers require them for warranty claims.",
            Icon = ""
        },
        new HvacTip
        {
            Category = "Filters",
            Title = "Filter Replacement",
            Content = "Standard filters should be changed every 1-3 months. HEPA filters last 6-12 months.",
            Icon = ""
        },
        new HvacTip
        {
            Category = "Sizing",
            Title = "BTU Matching",
            Content = "Oversized units short-cycle and undersized units run constantly. Proper BTU calculation prevents callbacks.",
            Icon = ""
        },
        new HvacTip
        {
            Category = "Diagnostics",
            Title = "Refrigerant Levels",
            Content = "Low refrigerant usually indicates a leak. Don't just recharge - find and fix the leak first.",
            Icon = ""
        }
    ];

    /// <summary>
    /// Gets a random HVAC tip.
    /// </summary>
    public static HvacTip GetRandomTip()
    {
        return HvacTips[Random.Shared.Next(HvacTips.Count)];
    }

    /// <summary>
    /// Resets the quick start progress (for testing).
    /// </summary>
    public void Reset()
    {
        _settings = new QuickStartSettings();
        SaveSettings();
    }

    private QuickStartSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<QuickStartSettings>(json) ?? new QuickStartSettings();
            }
        }
        catch
        {
            // Ignore load errors
        }
        return new QuickStartSettings();
    }

    private void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }
}

/// <summary>
/// Settings for quick start tracking.
/// </summary>
public class QuickStartSettings
{
    public bool HasCompletedOnboarding { get; set; }
    public bool HasDismissedWelcome { get; set; }
    public DateTime? OnboardingCompletedAt { get; set; }
    public List<string> CompletedSteps { get; set; } = [];
}

/// <summary>
/// Represents a quick start tutorial step.
/// </summary>
public class QuickStartStep
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "";
    public string NavigateTo { get; set; } = string.Empty;
}

/// <summary>
/// Represents an HVAC tip for display.
/// </summary>
public class HvacTip
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Icon { get; set; } = "";
}
