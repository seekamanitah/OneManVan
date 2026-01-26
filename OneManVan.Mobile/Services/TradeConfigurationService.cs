using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneManVan.Mobile.Services;

/// <summary>
/// Service for managing trade presets and configuration.
/// Allows switching between trades (HVAC, Plumbing, Electrical, etc.)
/// while maintaining data compatibility with desktop sync.
/// </summary>
public class TradeConfigurationService
{
    private const string CurrentTradeKey = "CurrentTrade";
    private const string SetupCompleteKey = "SetupComplete";
    private const string TradeConfigKey = "TradeConfig";
    
    private TradePreset? _currentPreset;
    private readonly string _presetsDirectory;

    public TradeConfigurationService()
    {
        _presetsDirectory = Path.Combine(FileSystem.AppDataDirectory, "Presets");
        Directory.CreateDirectory(_presetsDirectory);
    }

    /// <summary>
    /// Gets whether initial setup has been completed.
    /// </summary>
    public bool IsSetupComplete => Preferences.Get(SetupCompleteKey, false);

    /// <summary>
    /// Gets the currently selected trade type.
    /// </summary>
    public TradeType CurrentTrade => Enum.TryParse<TradeType>(
        Preferences.Get(CurrentTradeKey, TradeType.HVAC.ToString()), 
        out var trade) ? trade : TradeType.HVAC;

    /// <summary>
    /// Gets the current trade preset configuration.
    /// </summary>
    public TradePreset CurrentPreset
    {
        get
        {
            if (_currentPreset == null)
            {
                LoadCurrentPreset();
            }
            return _currentPreset ?? GetDefaultPreset(CurrentTrade);
        }
    }

    /// <summary>
    /// Marks initial setup as complete.
    /// </summary>
    public void CompleteSetup()
    {
        Preferences.Set(SetupCompleteKey, true);
    }

    /// <summary>
    /// Sets the current trade and loads its preset.
    /// </summary>
    public async Task SetTradeAsync(TradeType trade, bool keepExistingData = true)
    {
        Preferences.Set(CurrentTradeKey, trade.ToString());
        _currentPreset = null;
        
        // Load the preset for this trade
        var preset = await LoadPresetAsync(trade);
        _currentPreset = preset;
        
        // Save the config
        var json = JsonSerializer.Serialize(preset, new JsonSerializerOptions { WriteIndented = true });
        Preferences.Set(TradeConfigKey, json);
        
        // Notify listeners
        TradeChanged?.Invoke(this, trade);
    }

    /// <summary>
    /// Event raised when trade is changed.
    /// </summary>
    public event EventHandler<TradeType>? TradeChanged;

    /// <summary>
    /// Gets all available trade types.
    /// </summary>
    public IEnumerable<TradeInfo> GetAvailableTrades()
    {
        return new[]
        {
            new TradeInfo { Type = TradeType.HVAC, Name = "HVAC", Icon = "", Description = "Equipment tracking, BTU/SEER ratings, refrigerant types" },
            new TradeInfo { Type = TradeType.Plumbing, Name = "Plumbing", Icon = "", Description = "Fixture tracking, pipe materials, drain sizes" },
            new TradeInfo { Type = TradeType.Electrical, Name = "Electrical", Icon = "", Description = "Circuit tracking, amperage, voltage, wire gauges" },
            new TradeInfo { Type = TradeType.Appliance, Name = "Appliance Repair", Icon = "", Description = "Appliance types, energy ratings, diagnostics" },
            new TradeInfo { Type = TradeType.GeneralContractor, Name = "General Contractor", Icon = "", Description = "Flexible item tracking, room locations, square footage" },
            new TradeInfo { Type = TradeType.Locksmith, Name = "Locksmith", Icon = "", Description = "Lock types, security systems, key management" },
            new TradeInfo { Type = TradeType.Custom, Name = "Custom Trade", Icon = "", Description = "Create your own fields and templates" }
        };
    }

    /// <summary>
    /// Loads preset from embedded resources or file system.
    /// </summary>
    private async Task<TradePreset> LoadPresetAsync(TradeType trade)
    {
        // First check for custom preset file
        var customPath = Path.Combine(_presetsDirectory, $"{trade}Preset.json");
        if (File.Exists(customPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(customPath);
                var preset = JsonSerializer.Deserialize<TradePreset>(json);
                if (preset != null) return preset;
            }
            catch { }
        }

        // Return built-in preset
        return GetDefaultPreset(trade);
    }

    /// <summary>
    /// Saves a custom preset to file system.
    /// </summary>
    public async Task SavePresetAsync(TradePreset preset)
    {
        var path = Path.Combine(_presetsDirectory, $"{preset.TradeType}Preset.json");
        var json = JsonSerializer.Serialize(preset, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
        
        if (preset.TradeType == CurrentTrade)
        {
            _currentPreset = preset;
            Preferences.Set(TradeConfigKey, json);
        }
    }

    private void LoadCurrentPreset()
    {
        var json = Preferences.Get(TradeConfigKey, string.Empty);
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                _currentPreset = JsonSerializer.Deserialize<TradePreset>(json);
            }
            catch { }
        }
        
        _currentPreset ??= GetDefaultPreset(CurrentTrade);
    }

    /// <summary>
    /// Gets the default preset for a trade type.
    /// </summary>
    public TradePreset GetDefaultPreset(TradeType trade) => trade switch
    {
        TradeType.HVAC => CreateHvacPreset(),
        TradeType.Plumbing => CreatePlumbingPreset(),
        TradeType.Electrical => CreateElectricalPreset(),
        TradeType.Appliance => CreateAppliancePreset(),
        TradeType.GeneralContractor => CreateGeneralPreset(),
        TradeType.Locksmith => CreateLocksmithPreset(),
        _ => CreateCustomPreset()
    };

    #region Preset Factories

    private static TradePreset CreateHvacPreset() => new()
    {
        TradeType = TradeType.HVAC,
        Name = "HVAC",
        Description = "Heating, Ventilation & Air Conditioning",
        Icon = "",
        PrimaryColor = "#1976D2",
        AssetLabel = "Equipment",
        AssetPluralLabel = "Equipment",
        CustomFields = new List<TradeCustomField>
        {
            new() { EntityType = "Asset", FieldName = "FuelType", DisplayName = "Fuel Type", FieldType = "Enum", Options = "NaturalGas,Propane,Electric,Oil,DualFuel" },
            new() { EntityType = "Asset", FieldName = "RefrigerantType", DisplayName = "Refrigerant", FieldType = "Enum", Options = "R-22,R-410A,R-32,R-454B" },
            new() { EntityType = "Asset", FieldName = "BtuRating", DisplayName = "BTU Rating", FieldType = "Number" },
            new() { EntityType = "Asset", FieldName = "SeerRating", DisplayName = "SEER Rating", FieldType = "Decimal" },
            new() { EntityType = "Asset", FieldName = "TonnageX10", DisplayName = "Tonnage", FieldType = "Number" }
        },
        EstimateTemplates = new List<TradeTemplate>
        {
            new() { Name = "Furnace Tune-Up", Description = "Annual maintenance", BasePrice = 150, LaborHours = 1.5m },
            new() { Name = "AC Tune-Up", Description = "Seasonal AC service", BasePrice = 150, LaborHours = 1.5m },
            new() { Name = "System Diagnostic", Description = "Troubleshoot issue", BasePrice = 89, LaborHours = 1m },
            new() { Name = "Filter Replacement", Description = "Replace air filters", BasePrice = 45, LaborHours = 0.25m }
        }
    };

    private static TradePreset CreatePlumbingPreset() => new()
    {
        TradeType = TradeType.Plumbing,
        Name = "Plumbing",
        Description = "Plumbing & Water Systems",
        Icon = "",
        PrimaryColor = "#0288D1",
        AssetLabel = "Fixture",
        AssetPluralLabel = "Fixtures",
        CustomFields = new List<TradeCustomField>
        {
            new() { EntityType = "Asset", FieldName = "FixtureType", DisplayName = "Fixture Type", FieldType = "Enum", Options = "Faucet,Toilet,Shower,Bathtub,Sink,WaterHeater,Sump,Disposal" },
            new() { EntityType = "Asset", FieldName = "PipeType", DisplayName = "Pipe Material", FieldType = "Enum", Options = "Copper,PEX,PVC,Galvanized,Cast Iron" },
            new() { EntityType = "Asset", FieldName = "DrainSize", DisplayName = "Drain Size", FieldType = "Text" },
            new() { EntityType = "Asset", FieldName = "GallonCapacity", DisplayName = "Capacity (Gal)", FieldType = "Number" }
        },
        EstimateTemplates = new List<TradeTemplate>
        {
            new() { Name = "Drain Cleaning", Description = "Clear clogged drain", BasePrice = 150, LaborHours = 1m },
            new() { Name = "Faucet Repair", Description = "Fix leaky faucet", BasePrice = 125, LaborHours = 1m },
            new() { Name = "Toilet Repair", Description = "Toilet service", BasePrice = 175, LaborHours = 1m },
            new() { Name = "Water Heater Flush", Description = "Annual maintenance", BasePrice = 150, LaborHours = 1m }
        }
    };

    private static TradePreset CreateElectricalPreset() => new()
    {
        TradeType = TradeType.Electrical,
        Name = "Electrical",
        Description = "Electrical Systems & Wiring",
        Icon = "",
        PrimaryColor = "#FFC107",
        AssetLabel = "Circuit/Panel",
        AssetPluralLabel = "Circuits/Panels",
        CustomFields = new List<TradeCustomField>
        {
            new() { EntityType = "Asset", FieldName = "CircuitType", DisplayName = "Circuit Type", FieldType = "Enum", Options = "Breaker,Fuse,GFCI,AFCI,Main Panel,Sub Panel" },
            new() { EntityType = "Asset", FieldName = "Amperage", DisplayName = "Amperage", FieldType = "Number" },
            new() { EntityType = "Asset", FieldName = "Voltage", DisplayName = "Voltage", FieldType = "Enum", Options = "120V,240V,277V,480V" },
            new() { EntityType = "Asset", FieldName = "WireGauge", DisplayName = "Wire Gauge", FieldType = "Text" }
        },
        EstimateTemplates = new List<TradeTemplate>
        {
            new() { Name = "Outlet Install", Description = "Install new outlet", BasePrice = 150, LaborHours = 1m },
            new() { Name = "Switch Replacement", Description = "Replace switch", BasePrice = 95, LaborHours = 0.5m },
            new() { Name = "Panel Inspection", Description = "Safety inspection", BasePrice = 175, LaborHours = 1m },
            new() { Name = "GFCI Install", Description = "Install GFCI outlet", BasePrice = 175, LaborHours = 1m }
        }
    };

    private static TradePreset CreateAppliancePreset() => new()
    {
        TradeType = TradeType.Appliance,
        Name = "Appliance Repair",
        Description = "Home Appliance Service",
        Icon = "",
        PrimaryColor = "#7B1FA2",
        AssetLabel = "Appliance",
        AssetPluralLabel = "Appliances",
        CustomFields = new List<TradeCustomField>
        {
            new() { EntityType = "Asset", FieldName = "ApplianceType", DisplayName = "Appliance Type", FieldType = "Enum", Options = "Refrigerator,Washer,Dryer,Dishwasher,Oven,Microwave,Garbage Disposal" },
            new() { EntityType = "Asset", FieldName = "EnergyRating", DisplayName = "Energy Rating", FieldType = "Text" }
        },
        EstimateTemplates = new List<TradeTemplate>
        {
            new() { Name = "Diagnostic", Description = "Troubleshoot appliance", BasePrice = 89, LaborHours = 1m },
            new() { Name = "Minor Repair", Description = "Simple repair", BasePrice = 175, LaborHours = 1.5m },
            new() { Name = "Major Repair", Description = "Complex repair", BasePrice = 350, LaborHours = 3m }
        }
    };

    private static TradePreset CreateGeneralPreset() => new()
    {
        TradeType = TradeType.GeneralContractor,
        Name = "General Contractor",
        Description = "General Home Services",
        Icon = "",
        PrimaryColor = "#795548",
        AssetLabel = "Item",
        AssetPluralLabel = "Items",
        CustomFields = new List<TradeCustomField>
        {
            new() { EntityType = "Asset", FieldName = "ItemType", DisplayName = "Item Type", FieldType = "Text" },
            new() { EntityType = "Asset", FieldName = "RoomLocation", DisplayName = "Room/Location", FieldType = "Text" },
            new() { EntityType = "Asset", FieldName = "SquareFootage", DisplayName = "Sq Footage", FieldType = "Number" }
        },
        EstimateTemplates = new List<TradeTemplate>
        {
            new() { Name = "Consultation", Description = "On-site consultation", BasePrice = 75, LaborHours = 1m },
            new() { Name = "Minor Repair", Description = "Small repairs", BasePrice = 150, LaborHours = 2m },
            new() { Name = "Major Project", Description = "Large project", BasePrice = 500, LaborHours = 8m }
        }
    };

    private static TradePreset CreateLocksmithPreset() => new()
    {
        TradeType = TradeType.Locksmith,
        Name = "Locksmith",
        Description = "Lock & Security Services",
        Icon = "",
        PrimaryColor = "#455A64",
        AssetLabel = "Lock/Entry",
        AssetPluralLabel = "Locks/Entries",
        CustomFields = new List<TradeCustomField>
        {
            new() { EntityType = "Asset", FieldName = "LockType", DisplayName = "Lock Type", FieldType = "Enum", Options = "Deadbolt,Knob,Lever,Padlock,Electronic,Smart Lock" },
            new() { EntityType = "Asset", FieldName = "SecurityGrade", DisplayName = "Security Grade", FieldType = "Enum", Options = "Grade 1,Grade 2,Grade 3,Commercial" },
            new() { EntityType = "Asset", FieldName = "KeyType", DisplayName = "Key Type", FieldType = "Text" }
        },
        EstimateTemplates = new List<TradeTemplate>
        {
            new() { Name = "Lockout Service", Description = "Emergency unlock", BasePrice = 75, LaborHours = 0.5m },
            new() { Name = "Rekey Lock", Description = "Rekey existing lock", BasePrice = 50, LaborHours = 0.25m },
            new() { Name = "Lock Install", Description = "Install new lock", BasePrice = 150, LaborHours = 1m },
            new() { Name = "Smart Lock Setup", Description = "Smart lock installation", BasePrice = 200, LaborHours = 1.5m }
        }
    };

    private static TradePreset CreateCustomPreset() => new()
    {
        TradeType = TradeType.Custom,
        Name = "Custom Trade",
        Description = "Define your own trade",
        Icon = "",
        PrimaryColor = "#607D8B",
        AssetLabel = "Asset",
        AssetPluralLabel = "Assets",
        CustomFields = new List<TradeCustomField>(),
        EstimateTemplates = new List<TradeTemplate>()
    };

    #endregion
}

#region Models

/// <summary>
/// Supported trade types.
/// </summary>
public enum TradeType
{
    HVAC,
    Plumbing,
    Electrical,
    Appliance,
    GeneralContractor,
    Locksmith,
    Custom
}

/// <summary>
/// Information about a trade for display.
/// </summary>
public class TradeInfo
{
    public TradeType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Complete trade preset configuration.
/// </summary>
public class TradePreset
{
    public TradeType TradeType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string PrimaryColor { get; set; } = "#1976D2";
    
    /// <summary>
    /// Label for assets in this trade (e.g., "Equipment" for HVAC, "Fixture" for Plumbing).
    /// </summary>
    public string AssetLabel { get; set; } = "Asset";
    public string AssetPluralLabel { get; set; } = "Assets";
    
    /// <summary>
    /// Custom fields specific to this trade.
    /// </summary>
    public List<TradeCustomField> CustomFields { get; set; } = [];
    
    /// <summary>
    /// Estimate templates for this trade.
    /// </summary>
    public List<TradeTemplate> EstimateTemplates { get; set; } = [];
}

/// <summary>
/// Custom field definition for a trade.
/// </summary>
public class TradeCustomField
{
    public string EntityType { get; set; } = "Asset";
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FieldType { get; set; } = "Text"; // Text, Number, Decimal, Enum, Date, Boolean
    public string? Options { get; set; } // Comma-separated for Enum type
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public string? HelpText { get; set; }
}

/// <summary>
/// Estimate template for a trade.
/// </summary>
public class TradeTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public decimal LaborHours { get; set; }
    public List<string> IncludedItems { get; set; } = [];
}

#endregion

