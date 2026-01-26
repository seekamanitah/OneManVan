namespace OneManVan.Shared.Models.Enums;

/// <summary>
/// Type of job/service call.
/// </summary>
public enum JobType
{
    ServiceCall = 0,        // General service call
    Repair = 1,             // Specific repair
    Maintenance = 2,        // Preventive maintenance
    Installation = 3,       // New equipment install
    Replacement = 4,        // Equipment replacement
    Inspection = 5,         // System inspection
    Emergency = 6,          // Emergency service
    Warranty = 7,           // Warranty work
    Callback = 8,           // Return visit for previous job
    Estimate = 9,           // On-site estimate only
    Ductwork = 10,          // Duct cleaning/repair/install
    StartUp = 11,           // System start-up/commissioning
    Other = 99
}

/// <summary>
/// Priority level for job scheduling.
/// </summary>
public enum JobPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3,
    Emergency = 4
}

/// <summary>
/// Type of photo attachment.
/// </summary>
public enum PhotoType
{
    Other = 0,
    Before = 1,             // Before work photo
    After = 2,              // After work photo
    Issue = 3,              // Photo showing problem
    DataPlate = 4,          // Equipment data plate
    Signature = 5,          // Customer signature
    Serial = 6,             // Serial number photo
    FilterSize = 7,         // Filter size label
    Damage = 8,             // Damage documentation
    Installation = 9        // Installation photo
}

/// <summary>
/// Type of communication with customer.
/// </summary>
public enum CommunicationType
{
    Other = 0,
    PhoneCall = 1,
    Email = 2,
    Text = 3,
    InPerson = 4,
    Voicemail = 5,
    Letter = 6
}

/// <summary>
/// Direction of communication.
/// </summary>
public enum CommunicationDirection
{
    Inbound = 0,
    Outbound = 1
}
