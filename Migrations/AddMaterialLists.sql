-- =====================================================
-- Add Material Lists Tables Migration (SQLite)
-- Version: 2024.03
-- Description: Creates tables for HVAC material list 
--              bidding and tracking feature
-- =====================================================

-- =====================================================
-- 1. MaterialListTemplates (reusable templates)
-- =====================================================
CREATE TABLE IF NOT EXISTS MaterialListTemplates (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT,
    SystemType INTEGER NOT NULL DEFAULT 4,
    IsBuiltIn INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    UpdatedAt TEXT,
    CreatedBy TEXT
);

CREATE INDEX IF NOT EXISTS IX_MaterialListTemplates_Name ON MaterialListTemplates(Name);
CREATE INDEX IF NOT EXISTS IX_MaterialListTemplates_SystemType ON MaterialListTemplates(SystemType);
CREATE INDEX IF NOT EXISTS IX_MaterialListTemplates_IsBuiltIn ON MaterialListTemplates(IsBuiltIn);
CREATE INDEX IF NOT EXISTS IX_MaterialListTemplates_IsActive ON MaterialListTemplates(IsActive);

-- =====================================================
-- 2. MaterialListTemplateItems
-- =====================================================
CREATE TABLE IF NOT EXISTS MaterialListTemplateItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MaterialListTemplateId INTEGER NOT NULL,
    Category INTEGER NOT NULL DEFAULT 0,
    SubCategory TEXT,
    ItemName TEXT NOT NULL,
    Size TEXT,
    DefaultQuantity REAL NOT NULL DEFAULT 0,
    Unit TEXT NOT NULL DEFAULT 'each',
    DefaultUnitCost REAL NOT NULL DEFAULT 0,
    InventoryItemId INTEGER,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    IsRequired INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (MaterialListTemplateId) REFERENCES MaterialListTemplates(Id) ON DELETE CASCADE,
    FOREIGN KEY (InventoryItemId) REFERENCES InventoryItems(Id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS IX_MaterialListTemplateItems_TemplateId ON MaterialListTemplateItems(MaterialListTemplateId);
CREATE INDEX IF NOT EXISTS IX_MaterialListTemplateItems_Category ON MaterialListTemplateItems(Category);
CREATE INDEX IF NOT EXISTS IX_MaterialListTemplateItems_SortOrder ON MaterialListTemplateItems(SortOrder);

-- =====================================================
-- 3. MaterialLists (main entity)
-- =====================================================
CREATE TABLE IF NOT EXISTS MaterialLists (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ListNumber TEXT UNIQUE,
    Title TEXT NOT NULL,
    CustomerId INTEGER NOT NULL,
    SiteId INTEGER,
    JobId INTEGER,
    EstimateId INTEGER,
    SquareFootage INTEGER,
    Zones INTEGER NOT NULL DEFAULT 1,
    Stories INTEGER NOT NULL DEFAULT 1,
    Status INTEGER NOT NULL DEFAULT 0,
    FinalizedAt TEXT,
    FinalizedBy TEXT,
    TotalMaterialCost REAL NOT NULL DEFAULT 0,
    MarkupPercent REAL NOT NULL DEFAULT 0,
    TotalWithMarkup REAL NOT NULL DEFAULT 0,
    -- Labor Calculation
    LaborHours REAL,
    LaborHourlyRate REAL,
    LaborFixedPrice REAL,
    LaborTotal REAL NOT NULL DEFAULT 0,
    ContingencyPercent REAL NOT NULL DEFAULT 0,
    TaxPercent REAL NOT NULL DEFAULT 0,
    -- Disposal & Permits
    DisposalFee REAL NOT NULL DEFAULT 0,
    RefrigerantReclaimFee REAL NOT NULL DEFAULT 0,
    PermitCost REAL NOT NULL DEFAULT 0,
    InspectionFees REAL NOT NULL DEFAULT 0,
    PermitType TEXT,
    InspectionDate TEXT,
    -- Priority
    Priority INTEGER NOT NULL DEFAULT 1,
    -- Financial Summary
    AccessoriesTotal REAL NOT NULL DEFAULT 0,
    ContingencyAmount REAL NOT NULL DEFAULT 0,
    TaxAmount REAL NOT NULL DEFAULT 0,
    TotalBidPrice REAL NOT NULL DEFAULT 0,
    -- Notes
    Notes TEXT,
    InternalNotes TEXT,
    LaborNotes TEXT,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    UpdatedAt TEXT,
    CreatedBy TEXT,
    LastModifiedBy TEXT,
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE CASCADE,
    FOREIGN KEY (SiteId) REFERENCES Sites(Id) ON DELETE SET NULL,
    FOREIGN KEY (JobId) REFERENCES Jobs(Id) ON DELETE SET NULL,
    FOREIGN KEY (EstimateId) REFERENCES Estimates(Id) ON DELETE SET NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_MaterialLists_ListNumber ON MaterialLists(ListNumber);
CREATE INDEX IF NOT EXISTS IX_MaterialLists_CustomerId ON MaterialLists(CustomerId);
CREATE INDEX IF NOT EXISTS IX_MaterialLists_SiteId ON MaterialLists(SiteId);
CREATE INDEX IF NOT EXISTS IX_MaterialLists_Status ON MaterialLists(Status);
CREATE INDEX IF NOT EXISTS IX_MaterialLists_CreatedAt ON MaterialLists(CreatedAt);

-- =====================================================
-- 4. MaterialListSystems (multi-zone support)
-- =====================================================
CREATE TABLE IF NOT EXISTS MaterialListSystems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MaterialListId INTEGER NOT NULL,
    Name TEXT NOT NULL DEFAULT 'System 1',
    SortOrder INTEGER NOT NULL DEFAULT 1,
    SystemType INTEGER NOT NULL DEFAULT 4,
    IsTrunkSystem INTEGER NOT NULL DEFAULT 0,
    PlenumMaterial INTEGER,
    MainTrunkSize INTEGER,
    EquipmentModel TEXT,
    Tonnage REAL,
    SeerRating INTEGER,
    BtuRating INTEGER,
    SquareFootage INTEGER,
    Notes TEXT,
    FOREIGN KEY (MaterialListId) REFERENCES MaterialLists(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_MaterialListSystems_MaterialListId ON MaterialListSystems(MaterialListId);
CREATE INDEX IF NOT EXISTS IX_MaterialListSystems_SortOrder ON MaterialListSystems(SortOrder);

-- =====================================================
-- 5. MaterialListItems (line items)
-- =====================================================
CREATE TABLE IF NOT EXISTS MaterialListItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MaterialListId INTEGER NOT NULL,
    MaterialListSystemId INTEGER,
    Category INTEGER NOT NULL DEFAULT 0,
    SubCategory TEXT,
    ItemName TEXT NOT NULL,
    Size TEXT,
    Description TEXT,
    Quantity REAL NOT NULL DEFAULT 0,
    Unit TEXT NOT NULL DEFAULT 'each',
    UnitCost REAL NOT NULL DEFAULT 0,
    IsCostOverridden INTEGER NOT NULL DEFAULT 0,
    OriginalCost REAL,
    InventoryItemId INTEGER,
    IsFromInventory INTEGER NOT NULL DEFAULT 0,
    IsCustomItem INTEGER NOT NULL DEFAULT 0,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    Notes TEXT,
    FOREIGN KEY (MaterialListId) REFERENCES MaterialLists(Id) ON DELETE CASCADE,
    FOREIGN KEY (MaterialListSystemId) REFERENCES MaterialListSystems(Id) ON DELETE SET NULL,
    FOREIGN KEY (InventoryItemId) REFERENCES InventoryItems(Id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS IX_MaterialListItems_MaterialListId ON MaterialListItems(MaterialListId);
CREATE INDEX IF NOT EXISTS IX_MaterialListItems_MaterialListSystemId ON MaterialListItems(MaterialListSystemId);
CREATE INDEX IF NOT EXISTS IX_MaterialListItems_Category ON MaterialListItems(Category);
CREATE INDEX IF NOT EXISTS IX_MaterialListItems_SortOrder ON MaterialListItems(SortOrder);

-- =====================================================
-- 6. Insert Built-in HVAC Templates
-- =====================================================

-- Template 1: Standard Residential Split System
INSERT OR IGNORE INTO MaterialListTemplates (Id, Name, Description, SystemType, IsBuiltIn, IsActive, SortOrder)
VALUES (1, 'Standard Residential Split System', 'Complete material list for typical residential split system installation', 4, 1, 1, 1);

-- Template 2: Heat Pump System
INSERT OR IGNORE INTO MaterialListTemplates (Id, Name, Description, SystemType, IsBuiltIn, IsActive, SortOrder)
VALUES (2, 'Heat Pump Installation', 'Material list for heat pump system installation', 2, 1, 1, 2);

-- Template 3: Gas Furnace + AC
INSERT OR IGNORE INTO MaterialListTemplates (Id, Name, Description, SystemType, IsBuiltIn, IsActive, SortOrder)
VALUES (3, 'Gas Furnace with AC', 'Material list for gas furnace and AC combination', 3, 1, 1, 3);

-- Template 4: Ductwork Only
INSERT OR IGNORE INTO MaterialListTemplates (Id, Name, Description, SystemType, IsBuiltIn, IsActive, SortOrder)
VALUES (4, 'Ductwork Replacement', 'Material list for ductwork replacement only (no equipment)', 4, 1, 1, 4);

-- =====================================================
-- 7. Insert Template Items for Split System Template
-- =====================================================

-- Flex Duct Items
INSERT OR IGNORE INTO MaterialListTemplateItems (Id, MaterialListTemplateId, Category, SubCategory, ItemName, Size, DefaultQuantity, Unit, SortOrder)
VALUES 
    (1, 1, 0, 'FlexDuct', 'Flex Duct', '5"', 0, 'box', 1),
    (2, 1, 0, 'FlexDuct', 'Flex Duct', '6"', 0, 'box', 2),
    (3, 1, 0, 'FlexDuct', 'Flex Duct', '7"', 0, 'box', 3),
    (4, 1, 0, 'FlexDuct', 'Flex Duct', '8"', 0, 'box', 4),
    (5, 1, 0, 'FlexDuct', 'Flex Duct', '10"', 0, 'box', 5),
    (6, 1, 0, 'FlexDuct', 'Flex Duct', '12"', 0, 'box', 6),
    (7, 1, 0, 'FlexDuct', 'Flex Duct', '14"', 0, 'box', 7);

-- Take Offs
INSERT OR IGNORE INTO MaterialListTemplateItems (Id, MaterialListTemplateId, Category, SubCategory, ItemName, Size, DefaultQuantity, Unit, SortOrder)
VALUES 
    (10, 1, 0, 'TakeOffs', 'Round Take Off', '5"', 0, 'each', 10),
    (11, 1, 0, 'TakeOffs', 'Round Take Off', '6"', 0, 'each', 11),
    (12, 1, 0, 'TakeOffs', 'Round Take Off', '7"', 0, 'each', 12),
    (13, 1, 0, 'TakeOffs', 'Round Take Off', '8"', 0, 'each', 13),
    (14, 1, 0, 'TakeOffs', 'Round Take Off', '10"', 0, 'each', 14),
    (15, 1, 0, 'TakeOffs', 'Round Take Off', '12"', 0, 'each', 15),
    (16, 1, 0, 'TakeOffs', 'Round Take Off', '14"', 0, 'each', 16);

-- Floor Boots
INSERT OR IGNORE INTO MaterialListTemplateItems (Id, MaterialListTemplateId, Category, SubCategory, ItemName, Size, DefaultQuantity, Unit, SortOrder)
VALUES 
    (20, 1, 0, 'FloorBoots', 'Floor Boot', '4x10x5', 0, 'each', 20),
    (21, 1, 0, 'FloorBoots', 'Floor Boot', '4x10x6', 0, 'each', 21),
    (22, 1, 0, 'FloorBoots', 'Floor Boot', '4x12x7', 0, 'each', 22),
    (23, 1, 0, 'FloorBoots', 'Floor Boot', '4x12x8', 0, 'each', 23);

-- Returns
INSERT OR IGNORE INTO MaterialListTemplateItems (Id, MaterialListTemplateId, Category, SubCategory, ItemName, Size, DefaultQuantity, Unit, SortOrder)
VALUES 
    (30, 1, 0, 'Returns', 'Return Duct', '14"', 0, 'box', 30),
    (31, 1, 0, 'Returns', 'Return Duct', '16"', 0, 'box', 31),
    (32, 1, 0, 'Returns', 'Return Duct', '18"', 0, 'box', 32),
    (33, 1, 0, 'Returns', 'Return Duct', '20"', 0, 'box', 33);

-- Trunk Line
INSERT OR IGNORE INTO MaterialListTemplateItems (Id, MaterialListTemplateId, Category, SubCategory, ItemName, Size, DefaultQuantity, Unit, SortOrder)
VALUES 
    (40, 1, 0, 'TrunkLine', 'Trunk Line - Metal', '', 0, 'ft', 40),
    (41, 1, 0, 'TrunkLine', 'Trunk Line - Ductboard', '', 0, 'ft', 41),
    (42, 1, 0, 'Plenums', 'Supply Plenum', '', 0, 'each', 42),
    (43, 1, 0, 'Plenums', 'Return Plenum', '', 0, 'each', 43);

-- Equipment
INSERT OR IGNORE INTO MaterialListTemplateItems (Id, MaterialListTemplateId, Category, SubCategory, ItemName, Size, DefaultQuantity, Unit, SortOrder)
VALUES 
    (50, 1, 1, 'Equipment', 'Condenser Unit', '', 0, 'each', 50),
    (51, 1, 1, 'Equipment', 'Air Handler / Furnace', '', 0, 'each', 51),
    (52, 1, 1, 'Equipment', 'Condenser Pad', '', 0, 'each', 52);

-- Refrigerant/Copper
INSERT OR IGNORE INTO MaterialListTemplateItems (Id, MaterialListTemplateId, Category, SubCategory, ItemName, Size, DefaultQuantity, Unit, SortOrder)
VALUES 
    (60, 1, 3, 'Copper', 'Line Set', '3/8 x 3/4', 0, 'ft', 60),
    (61, 1, 3, 'Copper', 'Line Set', '3/8 x 7/8', 0, 'ft', 61);

-- Electrical
INSERT OR IGNORE INTO MaterialListTemplateItems (Id, MaterialListTemplateId, Category, SubCategory, ItemName, Size, DefaultQuantity, Unit, SortOrder)
VALUES 
    (70, 1, 2, 'Electrical', 'Disconnect Box', '60A', 0, 'each', 70),
    (71, 1, 2, 'Electrical', 'Whip', '6 ft', 0, 'each', 71),
    (72, 1, 2, 'Electrical', 'THHN Wire', '10 gauge', 0, 'ft', 72),
    (73, 1, 2, 'Electrical', 'Breaker', '30A', 0, 'each', 73);

-- Drain
INSERT OR IGNORE INTO MaterialListTemplateItems (Id, MaterialListTemplateId, Category, SubCategory, ItemName, Size, DefaultQuantity, Unit, SortOrder)
VALUES 
    (80, 1, 4, 'Drain', 'PVC Drain Line', '3/4"', 0, 'ft', 80),
    (81, 1, 4, 'Drain', 'Condensate Trap', '', 0, 'each', 81),
    (82, 1, 4, 'Drain', 'Drain Pan', '', 0, 'each', 82),
    (83, 1, 4, 'Drain', 'Condensate Pump', '', 0, 'each', 83);

-- Misc Materials
INSERT OR IGNORE INTO MaterialListTemplateItems (Id, MaterialListTemplateId, Category, SubCategory, ItemName, Size, DefaultQuantity, Unit, SortOrder)
VALUES 
    (90, 1, 5, 'Misc', 'Metal Tape', '', 0, 'roll', 90),
    (91, 1, 5, 'Misc', 'Thermostat Wire', '18/5', 0, 'ft', 91),
    (92, 1, 5, 'Misc', 'Hanging Strap', '1"', 0, 'ft', 92),
    (93, 1, 5, 'Misc', 'Thermostat', 'Programmable', 0, 'each', 93),
    (94, 1, 5, 'Misc', 'Duct Sealant', '', 0, 'each', 94),
    (95, 1, 5, 'Misc', 'Insulation Wrap', '', 0, 'sqft', 95),
    (96, 1, 5, 'Misc', 'Sheet Metal Screws', '', 0, 'box', 96),
    (97, 1, 5, 'Misc', 'Zip Ties', '', 0, 'pack', 97);

-- Grills/Registers - Floor Supply (Category 6 = GrillsRegisters)
INSERT OR IGNORE INTO MaterialListTemplateItems (Id, MaterialListTemplateId, Category, SubCategory, ItemName, Size, DefaultQuantity, Unit, SortOrder)
VALUES 
    (100, 1, 6, 'FloorSupply', 'Floor Register', '4x10', 0, 'each', 100),
    (101, 1, 6, 'FloorSupply', 'Floor Register', '4x12', 0, 'each', 101),
    (102, 1, 6, 'FloorSupply', 'Floor Register', '2x10', 0, 'each', 102),
    (103, 1, 6, 'FloorSupply', 'Floor Register', '6x10', 0, 'each', 103),
    (104, 1, 6, 'FloorSupply', 'Floor Register', '6x12', 0, 'each', 104);

-- Grills/Registers - Ceiling/Wall Supply
INSERT OR IGNORE INTO MaterialListTemplateItems (Id, MaterialListTemplateId, Category, SubCategory, ItemName, Size, DefaultQuantity, Unit, SortOrder)
VALUES 
    (110, 1, 6, 'CeilingWallSupply', 'Ceiling/Wall Register', '4x10', 0, 'each', 110),
    (111, 1, 6, 'CeilingWallSupply', 'Ceiling/Wall Register', '4x12', 0, 'each', 111),
    (112, 1, 6, 'CeilingWallSupply', 'Ceiling/Wall Register', '6x10', 0, 'each', 112),
    (113, 1, 6, 'CeilingWallSupply', 'Ceiling/Wall Register', '6x12', 0, 'each', 113),
    (114, 1, 6, 'CeilingWallSupply', 'Ceiling/Wall Register', '10x10', 0, 'each', 114),
    (115, 1, 6, 'CeilingWallSupply', 'Ceiling/Wall Register', '10x4', 0, 'each', 115);

-- Grills/Registers - Return Grilles (with filter slot)
INSERT OR IGNORE INTO MaterialListTemplateItems (Id, MaterialListTemplateId, Category, SubCategory, ItemName, Size, DefaultQuantity, Unit, SortOrder)
VALUES 
    (120, 1, 6, 'ReturnGrilles', 'Return Grille w/Filter', '20x20', 0, 'each', 120),
    (121, 1, 6, 'ReturnGrilles', 'Return Grille w/Filter', '20x25', 0, 'each', 121),
    (122, 1, 6, 'ReturnGrilles', 'Return Grille w/Filter', '20x30', 0, 'each', 122),
    (123, 1, 6, 'ReturnGrilles', 'Return Grille w/Filter', '14x14', 0, 'each', 123),
    (124, 1, 6, 'ReturnGrilles', 'Return Grille w/Filter', '16x20', 0, 'each', 124),
    (125, 1, 6, 'ReturnGrilles', 'Return Grille w/Filter', '18x18', 0, 'each', 125),
    (126, 1, 6, 'ReturnGrilles', 'Return Grille w/Filter', '24x20', 0, 'each', 126),
    (127, 1, 6, 'ReturnGrilles', 'Return Grille w/Filter', '24x24', 0, 'each', 127),
    (128, 1, 6, 'ReturnGrilles', 'Return Grille w/Filter', '30x12', 0, 'each', 128),
    (129, 1, 6, 'ReturnGrilles', 'Return Grille w/Filter', '30x20', 0, 'each', 129);

-- Sealing & Taping (Ductwork subcategory)
INSERT OR IGNORE INTO MaterialListTemplateItems (Id, MaterialListTemplateId, Category, SubCategory, ItemName, Size, DefaultQuantity, Unit, SortOrder)
VALUES 
    (130, 1, 0, 'SealingTaping', 'UL 181A Foil Tape', '', 0, 'roll', 130),
    (131, 1, 0, 'SealingTaping', 'Mastic Sealant', '', 0, 'each', 131),
    (132, 1, 0, 'SealingTaping', 'Duct Sealant Caulk', '', 0, 'each', 132),
    (133, 1, 0, 'SealingTaping', 'Cork Tape', '', 0, 'roll', 133),
    (134, 1, 0, 'SealingTaping', 'Foam Tape', '', 0, 'roll', 134);

-- Support & Hanging (Ductwork subcategory)
INSERT OR IGNORE INTO MaterialListTemplateItems (Id, MaterialListTemplateId, Category, SubCategory, ItemName, Size, DefaultQuantity, Unit, SortOrder)
VALUES 
    (140, 1, 0, 'SupportHanging', 'Duct Hangers Metal', '', 0, 'each', 140),
    (141, 1, 0, 'SupportHanging', 'Perforated Strap', '', 0, 'ft', 141),
    (142, 1, 0, 'SupportHanging', 'Heavy-Duty Zip Ties', '', 0, 'pack', 142),
    (143, 1, 0, 'SupportHanging', 'Zip Screws', '', 0, 'box', 143);

-- Insulation (Ductwork subcategory)
INSERT OR IGNORE INTO MaterialListTemplateItems (Id, MaterialListTemplateId, Category, SubCategory, ItemName, Size, DefaultQuantity, Unit, SortOrder)
VALUES 
    (150, 1, 0, 'Insulation', 'Duct Wrap Foil-Faced Fiberglass', '', 0, 'sqft', 150),
    (151, 1, 0, 'Insulation', 'External Duct Insulation', '', 0, 'sqft', 151);

-- Fittings & Accessories (Ductwork subcategory)
INSERT OR IGNORE INTO MaterialListTemplateItems (Id, MaterialListTemplateId, Category, SubCategory, ItemName, Size, DefaultQuantity, Unit, SortOrder)
VALUES 
    (160, 1, 0, 'Fittings', 'Metal Collar/Sleeve', '4"+', 0, 'each', 160),
    (161, 1, 0, 'Fittings', 'Reducer/Transition', '', 0, 'each', 161),
    (162, 1, 0, 'Fittings', 'Elbow 90 Degree', '', 0, 'each', 162),
    (163, 1, 0, 'Fittings', 'Elbow 45 Degree', '', 0, 'each', 163),
    (164, 1, 0, 'Fittings', 'Offset', '', 0, 'each', 164),
    (165, 1, 0, 'Fittings', 'Volume Damper', '', 0, 'each', 165),
    (166, 1, 0, 'Fittings', 'Fire Damper', '', 0, 'each', 166),
    (167, 1, 0, 'Fittings', 'Take-Off Collar', '', 0, 'each', 167),
    (168, 1, 0, 'Fittings', 'Starting Collar', '', 0, 'each', 168);

-- Accessories (Category 7)
INSERT OR IGNORE INTO MaterialListTemplateItems (Id, MaterialListTemplateId, Category, SubCategory, ItemName, Size, DefaultQuantity, Unit, SortOrder)
VALUES 
    (170, 1, 7, 'Accessories', 'Humidifier', '', 0, 'each', 170),
    (171, 1, 7, 'Accessories', 'UV Light', '', 0, 'each', 171),
    (172, 1, 7, 'Accessories', 'Air Cleaner', '', 0, 'each', 172),
    (173, 1, 7, 'Accessories', 'Bypass Damper', '', 0, 'each', 173);

-- Additional Flex Duct sizes (16" and 18")
INSERT OR IGNORE INTO MaterialListTemplateItems (Id, MaterialListTemplateId, Category, SubCategory, ItemName, Size, DefaultQuantity, Unit, SortOrder)
VALUES 
    (8, 1, 0, 'FlexDuct', 'Flex Duct', '16"', 0, 'box', 8),
    (9, 1, 0, 'FlexDuct', 'Flex Duct', '18"', 0, 'box', 9);

-- Additional Take Offs sizes (16", 18", 20")
INSERT OR IGNORE INTO MaterialListTemplateItems (Id, MaterialListTemplateId, Category, SubCategory, ItemName, Size, DefaultQuantity, Unit, SortOrder)
VALUES 
    (17, 1, 0, 'TakeOffs', 'Round Take Off', '16"', 0, 'each', 17),
    (18, 1, 0, 'TakeOffs', 'Round Take Off', '18"', 0, 'each', 18),
    (19, 1, 0, 'TakeOffs', 'Round Take Off', '20"', 0, 'each', 19);

SELECT 'Material Lists tables and templates created successfully' AS Status;
