namespace OneManVan.Shared.Models.Enums;

/// <summary>
/// Status of a material list.
/// </summary>
public enum MaterialListStatus
{
    /// <summary>
    /// List is being created/edited.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// List is complete and locked for editing (warning shown if edited).
    /// </summary>
    Finalized = 1
}

/// <summary>
/// HVAC system type for material calculations.
/// </summary>
public enum HvacSystemType
{
    /// <summary>
    /// Spider/radial duct system - single plenum with branches.
    /// </summary>
    Spider = 0,

    /// <summary>
    /// Trunk and branch system with main trunk line.
    /// </summary>
    Trunk = 1,

    /// <summary>
    /// Heat pump system.
    /// </summary>
    HeatPump = 2,

    /// <summary>
    /// Gas furnace system.
    /// </summary>
    GasFurnace = 3,

    /// <summary>
    /// Split system (AC + furnace/air handler).
    /// </summary>
    SplitSystem = 4,

    /// <summary>
    /// Package unit (all-in-one outdoor unit).
    /// </summary>
    PackageUnit = 5,

    /// <summary>
    /// Mini-split / ductless system.
    /// </summary>
    MiniSplit = 6,

    /// <summary>
    /// Other/custom system type.
    /// </summary>
    Other = 99
}

/// <summary>
/// Material category for grouping items.
/// </summary>
public enum MaterialCategory
{
    /// <summary>
    /// Ductwork materials (flex duct, take-offs, boots, returns, trunk).
    /// </summary>
    Ductwork = 0,

    /// <summary>
    /// HVAC equipment (units, handlers, etc.).
    /// </summary>
    Equipment = 1,

    /// <summary>
    /// Electrical materials (breakers, wire, conduit).
    /// </summary>
    Electrical = 2,

    /// <summary>
    /// Refrigerant and copper lines.
    /// </summary>
    Refrigerant = 3,

    /// <summary>
    /// Drain and plumbing materials.
    /// </summary>
    Drain = 4,

    /// <summary>
    /// Miscellaneous supplies (tape, sealant, screws).
    /// </summary>
    Miscellaneous = 5,

    /// <summary>
    /// Grills and registers (floor, ceiling, wall, return).
    /// </summary>
    GrillsRegisters = 6,

    /// <summary>
    /// Accessories (humidifier, UV light, air cleaner, dampers).
    /// </summary>
    Accessories = 7,

    /// <summary>
    /// Disposal and removal costs.
    /// </summary>
    Disposal = 8,

    /// <summary>
    /// Permits and inspection fees.
    /// </summary>
    Permits = 9,

    /// <summary>
    /// Custom/other items.
    /// </summary>
    Other = 99
}

/// <summary>
/// Ductwork subcategories.
/// </summary>
public enum DuctworkSubcategory
{
    FlexDuct = 0,
    TakeOffs = 1,
    FloorBoots = 2,
    Returns = 3,
    TrunkLine = 4,
    Plenums = 5,
    Fittings = 6,
    SealingTaping = 7,
    SupportHanging = 8,
    Insulation = 9,
    Other = 99
}

/// <summary>
/// Plenum/ductboard material type.
/// </summary>
public enum PlenumMaterial
{
    Ductboard = 0,
    Metal = 1,
    Fiber = 2
}
