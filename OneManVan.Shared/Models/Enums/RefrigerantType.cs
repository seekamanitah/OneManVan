namespace OneManVan.Shared.Models.Enums;

/// <summary>
/// Refrigerant types used in HVAC systems.
/// CRITICAL for service - determines handling requirements and availability.
/// </summary>
public enum RefrigerantType
{
    Unknown = 0,
    
    // Legacy (being phased out)
    R22 = 1,           // HCFC, phased out 2020, very expensive
    
    // Current Standard
    R410A = 2,         // HFC, most common in residential since ~2010
    R407C = 3,         // HFC, used in commercial
    R134a = 4,         // Used in some commercial/automotive
    
    // New Low-GWP Refrigerants
    R32 = 10,          // Lower GWP, used in mini-splits
    R454B = 11,        // Opteon XL41, R-410A replacement
    R452B = 12,        // Opteon XL55
    R466A = 13,        // Solstice N41
    
    // Natural Refrigerants
    R290 = 20,         // Propane, used in some mini-splits
    R744 = 21,         // CO2, commercial refrigeration
    R717 = 22,         // Ammonia, industrial
    
    // Other
    Other = 99
}
