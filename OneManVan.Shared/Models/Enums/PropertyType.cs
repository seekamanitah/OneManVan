namespace OneManVan.Shared.Models.Enums;

/// <summary>
/// Type of property/dwelling for site classification.
/// </summary>
public enum PropertyType
{
    Unknown = 0,
    // Residential
    SingleFamily = 1,
    MultiFamily = 2,
    Condo = 3,
    Townhouse = 4,
    MobileHome = 5,
    Apartment = 6,
    // Commercial
    Commercial = 10,
    Industrial = 11,
    Retail = 12,
    Office = 13,
    Restaurant = 14,
    Warehouse = 15,
    // Institutional
    Church = 20,
    School = 21,
    Government = 22,
    Hospital = 23,
    // Other
    NewConstruction = 30,
    Other = 99
}
