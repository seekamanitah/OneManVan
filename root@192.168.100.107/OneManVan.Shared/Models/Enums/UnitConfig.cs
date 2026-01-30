namespace OneManVan.Shared.Models.Enums;

/// <summary>
/// HVAC unit configuration enumeration for asset classification.
/// </summary>
public enum UnitConfig
{
    Unknown = 0,
    Split = 1,
    Packaged = 2,
    Furnace = 3,
    Coil = 4,
    Condenser = 5,
    HeatPump = 6,
    MiniSplit = 7,
    Boiler = 8
}
