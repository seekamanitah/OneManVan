namespace OneManVan.Shared.Models.Enums;

/// <summary>
/// Detailed HVAC equipment types for better categorization.
/// </summary>
public enum EquipmentType
{
    Unknown = 0,
    
    // Heating Equipment
    GasFurnace = 1,
    OilFurnace = 2,
    ElectricFurnace = 3,
    Boiler = 4,
    HotWaterHeater = 5,
    
    // Cooling Equipment
    AirConditioner = 10,
    HeatPump = 11,
    MiniSplit = 12,
    DuctlessMiniSplit = 13,
    WindowUnit = 14,
    
    // Package Units
    PackagedUnit = 20,      // Combined heating/cooling
    RooftopUnit = 21,       // Commercial rooftop
    PTAC = 22,              // Packaged terminal AC
    
    // Components
    Coil = 30,              // Evaporator coil
    Condenser = 31,         // Outdoor condensing unit
    AirHandler = 32,        // Indoor air handler
    FanCoil = 33,
    
    // Air Quality
    Humidifier = 40,
    Dehumidifier = 41,
    AirPurifier = 42,
    UVLight = 43,
    ERV = 44,               // Energy recovery ventilator
    HRV = 45,               // Heat recovery ventilator
    
    // Controls
    Thermostat = 50,
    ZoningSystem = 51,
    
    // Other
    Ductwork = 60,
    Other = 99
}

/// <summary>
/// Filter types for HVAC systems.
/// </summary>
public enum FilterType
{
    Unknown = 0,
    Standard = 1,           // Basic fiberglass
    Pleated = 2,            // Common pleated filters
    HighEfficiency = 3,     // MERV 11-13
    HEPA = 4,               // MERV 17+
    Electronic = 5,         // Electronic air cleaner
    Washable = 6,           // Reusable/washable
    MediaFilter = 7,        // 4-5" thick media filter
    Charcoal = 8            // Activated charcoal
}

/// <summary>
/// Thermostat types for compatibility tracking.
/// </summary>
public enum ThermostatType
{
    Unknown = 0,
    Manual = 1,                 // Basic non-programmable
    Programmable = 2,           // 7-day programmable
    SmartWifi = 3,              // Smart/connected thermostat
    Zoning = 4,                 // Zoning control panel
    CommercialProgrammable = 5, // Commercial controls
    HeatPumpSpecific = 6        // Heat pump compatible
}

/// <summary>
/// Condition rating for equipment.
/// </summary>
public enum AssetCondition
{
    Unknown = 0,
    Excellent = 1,          // Like new, no issues
    Good = 2,               // Normal wear, functioning well
    Fair = 3,               // Showing age, may need attention soon
    Poor = 4,               // Multiple issues, needs repair/replacement
    FailureImminent = 5,    // Will fail soon, replace recommended
    Failed = 6              // Not operational
}

/// <summary>
/// Status of asset in system.
/// </summary>
public enum AssetStatus
{
    Active = 0,             // Currently in service
    Inactive = 1,           // Not currently in use
    UnderRepair = 2,        // Being repaired
    Replaced = 3,           // Has been replaced
    Removed = 4,            // Removed from location
    PendingInstall = 5,     // Waiting to be installed
    Decommissioned = 6      // Permanently out of service
}
