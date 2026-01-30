namespace OneManVan.Shared.Models.Enums;

/// <summary>
/// Product category for HVAC equipment catalog.
/// </summary>
public enum ProductCategory
{
    Unknown = 0,
    AirConditioner = 1,
    HeatPump = 2,
    GasFurnace = 3,
    OilFurnace = 4,
    ElectricFurnace = 5,
    Boiler = 6,
    MiniSplit = 7,
    PackagedUnit = 8,
    AirHandler = 9,
    Coil = 10,
    Thermostat = 11,
    AirPurifier = 12,
    Humidifier = 13,
    Dehumidifier = 14,
    Accessories = 15
}

/// <summary>
/// Types of documents that can be attached to products.
/// </summary>
public enum ProductDocumentType
{
    Other = 0,
    Brochure = 1,
    Manual = 2,
    SpecSheet = 3,
    Nomenclature = 4,
    InstallGuide = 5,
    WiringDiagram = 6,
    PartsBreakdown = 7,
    WarrantyInfo = 8,
    ServiceBulletin = 9,
    TroubleshootingGuide = 10
}
