using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Site entity representing a service location for a customer.
/// </summary>
public class Site
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    /// <summary>
    /// Auto-generated site number (e.g., S-0001).
    /// </summary>
    [MaxLength(20)]
    public string? SiteNumber { get; set; }

    // === Address ===

    [Required]
    [MaxLength(300)]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Unit, Suite, Apartment number.
    /// </summary>
    [MaxLength(50)]
    public string? Address2 { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(50)]
    public string? State { get; set; }

    [MaxLength(20)]
    public string? ZipCode { get; set; }

    [MaxLength(50)]
    public string? Country { get; set; } = "USA";

    // === Geolocation (for routing) ===

    [Column(TypeName = "decimal(10,7)")]
    public decimal? Latitude { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal? Longitude { get; set; }

    /// <summary>
    /// Google Place ID for address validation.
    /// </summary>
    [MaxLength(200)]
    public string? GooglePlaceId { get; set; }

    /// <summary>
    /// Cross streets for easier navigation.
    /// </summary>
    [MaxLength(200)]
    public string? CrossStreets { get; set; }

    // === Property Details ===

    public PropertyType PropertyType { get; set; } = PropertyType.Unknown;

    public int? YearBuilt { get; set; }

    public int? SquareFootage { get; set; }

    public int? Stories { get; set; }

    // === Access Information ===

    [MaxLength(20)]
    public string? GateCode { get; set; }

    [MaxLength(20)]
    public string? LockboxCode { get; set; }

    [MaxLength(1000)]
    public string? AccessInstructions { get; set; }

    [MaxLength(500)]
    public string? ParkingInstructions { get; set; }

    public bool HasDog { get; set; } = false;

    public bool HasGate { get; set; } = false;

    public bool RequiresAppointment { get; set; } = false;

    [MaxLength(200)]
    public string? PetNotes { get; set; }

    // === Site Contact (if different from customer) ===

    [MaxLength(100)]
    public string? SiteContactName { get; set; }

    [MaxLength(20)]
    [Phone]
    public string? SiteContactPhone { get; set; }

    [MaxLength(100)]
    [EmailAddress]
    public string? SiteContactEmail { get; set; }

    // === Service Zone (for pricing/scheduling) ===

    /// <summary>
    /// Service zone identifier for pricing tiers.
    /// </summary>
    [MaxLength(50)]
    public string? ServiceZone { get; set; }

    /// <summary>
    /// Additional travel surcharge for this location.
    /// </summary>
    [Column(TypeName = "decimal(8,2)")]
    public decimal? TravelSurcharge { get; set; }

    /// <summary>
    /// Estimated drive time from shop in minutes.
    /// </summary>
    public int? EstimatedDriveMinutes { get; set; }

    // === Building System Locations ===

    [MaxLength(200)]
    public string? ElectricalPanelLocation { get; set; }

    [MaxLength(200)]
    public string? GasShutoffLocation { get; set; }

    [MaxLength(200)]
    public string? WaterShutoffLocation { get; set; }

    /// <summary>
    /// Where HVAC equipment is located (Basement, Attic, Closet, etc.).
    /// </summary>
    [MaxLength(200)]
    public string? HvacLocation { get; set; }

    [MaxLength(200)]
    public string? ThermostatLocation { get; set; }

    [MaxLength(200)]
    public string? FilterLocation { get; set; }

    // === General Notes ===

    /// <summary>
    /// Legacy field - kept for backward compatibility.
    /// </summary>
    [MaxLength(500)]
    public string? AccessNotes { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    // === Status ===

    public bool IsPrimary { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastServiceDate { get; set; }

    // === Navigation Properties ===

    public Customer Customer { get; set; } = null!;

    public ICollection<Asset> Assets { get; set; } = [];

    // === Computed Properties ===

    [NotMapped]
    public string FullAddress
    {
        get
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(Address))
                parts.Add(Address);
            if (!string.IsNullOrWhiteSpace(Address2))
                parts.Add(Address2);
            
            var cityStateZip = new List<string>();
            if (!string.IsNullOrWhiteSpace(City))
                cityStateZip.Add(City);
            if (!string.IsNullOrWhiteSpace(State))
                cityStateZip.Add(State);
            if (!string.IsNullOrWhiteSpace(ZipCode))
                cityStateZip.Add(ZipCode);
            
            if (cityStateZip.Count > 0)
                parts.Add(string.Join(", ", cityStateZip.Take(2)) + (cityStateZip.Count > 2 ? " " + cityStateZip[2] : ""));
            
            return string.Join(", ", parts);
        }
    }

    [NotMapped]
    public string ShortAddress => !string.IsNullOrWhiteSpace(Address2)
        ? $"{Address}, {Address2}"
        : Address;

    [NotMapped]
    public bool HasGeoLocation => Latitude.HasValue && Longitude.HasValue;

    [NotMapped]
    public string PropertyTypeDisplay => PropertyType switch
    {
        Enums.PropertyType.SingleFamily => "Single Family",
        Enums.PropertyType.MultiFamily => "Multi-Family",
        Enums.PropertyType.Condo => "Condo",
        Enums.PropertyType.Townhouse => "Townhouse",
        Enums.PropertyType.MobileHome => "Mobile Home",
        Enums.PropertyType.Apartment => "Apartment",
        Enums.PropertyType.Commercial => "Commercial",
        Enums.PropertyType.Industrial => "Industrial",
        Enums.PropertyType.Retail => "Retail",
        Enums.PropertyType.Office => "Office",
        Enums.PropertyType.Restaurant => "Restaurant",
        Enums.PropertyType.Warehouse => "Warehouse",
        Enums.PropertyType.Church => "Church",
        Enums.PropertyType.School => "School",
        Enums.PropertyType.Government => "Government",
        Enums.PropertyType.Hospital => "Hospital",
        Enums.PropertyType.NewConstruction => "New Construction",
        Enums.PropertyType.Other => "Other",
        _ => "Unknown"
    };

    [NotMapped]
    public string GoogleMapsUrl => HasGeoLocation
        ? $"https://www.google.com/maps?q={Latitude},{Longitude}"
        : $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(FullAddress)}";

    [NotMapped]
    public List<string> AccessWarnings
    {
        get
        {
            var warnings = new List<string>();
            if (HasDog) warnings.Add("?? Dog on premises");
            if (HasGate) warnings.Add("?? Gated access");
            if (RequiresAppointment) warnings.Add("?? Appointment required");
            if (!string.IsNullOrWhiteSpace(PetNotes)) warnings.Add($"?? {PetNotes}");
            return warnings;
        }
    }

    [NotMapped]
    public bool HasAccessCodes => !string.IsNullOrWhiteSpace(GateCode) || !string.IsNullOrWhiteSpace(LockboxCode);
}
