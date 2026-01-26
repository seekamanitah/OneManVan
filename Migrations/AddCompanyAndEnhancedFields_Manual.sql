-- =============================================
-- Field Enhancement Migration Script
-- Version: 1.0
-- Date: 2025-01-26
-- Description: Adds Company entity, enhanced Customer/Asset fields,
--              multi-ownership tables, and custom field enhancements
-- =============================================

BEGIN TRANSACTION;

-- =============================================
-- STEP 1: Create New Tables
-- =============================================

-- Companies table
CREATE TABLE IF NOT EXISTS Companies (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    LegalName TEXT NULL,
    TaxId TEXT NULL,
    Website TEXT NULL,
    Phone TEXT NULL,
    Email TEXT NULL,
    BillingAddress TEXT NULL,
    BillingCity TEXT NULL,
    BillingState TEXT NULL,
    BillingZipCode TEXT NULL,
    CompanyType INTEGER NOT NULL DEFAULT 0,
    Industry TEXT NULL,
    Notes TEXT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    UpdatedAt TEXT NULL
);

CREATE INDEX IX_Companies_Name ON Companies(Name);
CREATE INDEX IX_Companies_CompanyType ON Companies(CompanyType);
CREATE INDEX IX_Companies_IsActive ON Companies(IsActive);

-- AssetOwners junction table
CREATE TABLE IF NOT EXISTS AssetOwners (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    AssetId INTEGER NOT NULL,
    OwnerType TEXT NOT NULL,
    OwnerId INTEGER NOT NULL,
    OwnershipType TEXT NOT NULL DEFAULT 'Primary',
    StartDate TEXT NOT NULL DEFAULT (datetime('now')),
    EndDate TEXT NULL,
    Notes TEXT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (AssetId) REFERENCES Assets(Id) ON DELETE CASCADE
);

CREATE INDEX IX_AssetOwners_AssetId_OwnerId_OwnerType ON AssetOwners(AssetId, OwnerId, OwnerType);
CREATE INDEX IX_AssetOwners_IsActive ON AssetOwners(IsActive);

-- CustomerCompanyRoles junction table
CREATE TABLE IF NOT EXISTS CustomerCompanyRoles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId INTEGER NOT NULL,
    CompanyId INTEGER NOT NULL,
    Role TEXT NOT NULL DEFAULT 'Contact',
    Title TEXT NULL,
    Department TEXT NULL,
    IsPrimaryContact INTEGER NOT NULL DEFAULT 0,
    StartDate TEXT NOT NULL DEFAULT (datetime('now')),
    EndDate TEXT NULL,
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE CASCADE,
    FOREIGN KEY (CompanyId) REFERENCES Companies(Id) ON DELETE CASCADE
);

CREATE INDEX IX_CustomerCompanyRoles_CustomerId_CompanyId ON CustomerCompanyRoles(CustomerId, CompanyId);
CREATE INDEX IX_CustomerCompanyRoles_IsPrimaryContact ON CustomerCompanyRoles(IsPrimaryContact);

-- CustomFieldDefinitions enhanced table
CREATE TABLE IF NOT EXISTS CustomFieldDefinitions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EntityType TEXT NOT NULL,
    FieldName TEXT NOT NULL,
    DisplayName TEXT NOT NULL,
    FieldType TEXT NOT NULL DEFAULT 'Text',
    IsRequired INTEGER NOT NULL DEFAULT 0,
    IsReadOnly INTEGER NOT NULL DEFAULT 0,
    IsSystemField INTEGER NOT NULL DEFAULT 0,
    DefaultValue TEXT NULL,
    Placeholder TEXT NULL,
    HelpText TEXT NULL,
    ValidationRegex TEXT NULL,
    MinValue REAL NULL,
    MaxValue REAL NULL,
    MinLength INTEGER NULL,
    MaxLength INTEGER NULL,
    DisplayOrder INTEGER NOT NULL DEFAULT 0,
    GroupName TEXT NULL,
    IsVisible INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    UpdatedAt TEXT NULL
);

CREATE UNIQUE INDEX IX_CustomFieldDefinitions_EntityType_FieldName ON CustomFieldDefinitions(EntityType, FieldName);
CREATE INDEX IX_CustomFieldDefinitions_DisplayOrder ON CustomFieldDefinitions(DisplayOrder);
CREATE INDEX IX_CustomFieldDefinitions_IsVisible ON CustomFieldDefinitions(IsVisible);

-- CustomFieldChoices table
CREATE TABLE IF NOT EXISTS CustomFieldChoices (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FieldDefinitionId INTEGER NOT NULL,
    Value TEXT NOT NULL,
    DisplayText TEXT NOT NULL,
    Color TEXT NULL,
    Icon TEXT NULL,
    DisplayOrder INTEGER NOT NULL DEFAULT 0,
    IsDefault INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (FieldDefinitionId) REFERENCES CustomFieldDefinitions(Id) ON DELETE CASCADE
);

CREATE INDEX IX_CustomFieldChoices_FieldDefinitionId_DisplayOrder ON CustomFieldChoices(FieldDefinitionId, DisplayOrder);
CREATE INDEX IX_CustomFieldChoices_IsActive ON CustomFieldChoices(IsActive);

-- =============================================
-- STEP 2: Add New Columns to Existing Tables
-- =============================================

-- Customer enhancements
ALTER TABLE Customers ADD COLUMN FirstName TEXT NULL;
ALTER TABLE Customers ADD COLUMN LastName TEXT NULL;
ALTER TABLE Customers ADD COLUMN Mobile TEXT NULL;
ALTER TABLE Customers ADD COLUMN Website TEXT NULL;
ALTER TABLE Customers ADD COLUMN CompanyId INTEGER NULL REFERENCES Companies(Id) ON DELETE SET NULL;

-- Asset enhancements
ALTER TABLE Assets ADD COLUMN AssetName TEXT NULL;
ALTER TABLE Assets ADD COLUMN Description TEXT NULL;
ALTER TABLE Assets ADD COLUMN CompanyId INTEGER NULL REFERENCES Companies(Id) ON DELETE SET NULL;
ALTER TABLE Assets ADD COLUMN IsWarrantiedBySEHVAC INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Assets ADD COLUMN WarrantyExpiration TEXT NULL;
ALTER TABLE Assets ADD COLUMN Location TEXT NULL;

-- =============================================
-- STEP 3: Data Migration - Split Customer Names
-- =============================================

-- Update FirstName and LastName from existing Name field
UPDATE Customers 
SET FirstName = CASE 
    WHEN instr(Name, ' ') > 0 
    THEN substr(Name, 1, instr(Name, ' ') - 1)
    ELSE Name 
END,
LastName = CASE 
    WHEN instr(Name, ' ') > 0 
    THEN substr(Name, instr(Name, ' ') + 1)
    ELSE '' 
END
WHERE FirstName IS NULL AND LastName IS NULL;

-- =============================================
-- STEP 4: Seed Default Data
-- =============================================

-- Insert Brand choices for Assets
INSERT OR IGNORE INTO CustomFieldDefinitions (EntityType, FieldName, DisplayName, FieldType, IsSystemField, DisplayOrder)
VALUES ('Asset', 'Brand', 'Brand', 'Dropdown', 1, 1);

-- Get the FieldDefinitionId for Brand
INSERT INTO CustomFieldChoices (FieldDefinitionId, Value, DisplayText, DisplayOrder, IsActive)
SELECT 
    (SELECT Id FROM CustomFieldDefinitions WHERE EntityType = 'Asset' AND FieldName = 'Brand'),
    'carrier', 'Carrier', 1, 1
UNION ALL SELECT 
    (SELECT Id FROM CustomFieldDefinitions WHERE EntityType = 'Asset' AND FieldName = 'Brand'),
    'trane', 'Trane', 2, 1
UNION ALL SELECT 
    (SELECT Id FROM CustomFieldDefinitions WHERE EntityType = 'Asset' AND FieldName = 'Brand'),
    'lennox', 'Lennox', 3, 1
UNION ALL SELECT 
    (SELECT Id FROM CustomFieldDefinitions WHERE EntityType = 'Asset' AND FieldName = 'Brand'),
    'rheem', 'Rheem', 4, 1
UNION ALL SELECT 
    (SELECT Id FROM CustomFieldDefinitions WHERE EntityType = 'Asset' AND FieldName = 'Brand'),
    'goodman', 'Goodman', 5, 1
UNION ALL SELECT 
    (SELECT Id FROM CustomFieldDefinitions WHERE EntityType = 'Asset' AND FieldName = 'Brand'),
    'york', 'York', 6, 1
UNION ALL SELECT 
    (SELECT Id FROM CustomFieldDefinitions WHERE EntityType = 'Asset' AND FieldName = 'Brand'),
    'daikin', 'Daikin', 7, 1
UNION ALL SELECT 
    (SELECT Id FROM CustomFieldDefinitions WHERE EntityType = 'Asset' AND FieldName = 'Brand'),
    'mitsubishi', 'Mitsubishi', 8, 1
UNION ALL SELECT 
    (SELECT Id FROM CustomFieldDefinitions WHERE EntityType = 'Asset' AND FieldName = 'Brand'),
    'fujitsu', 'Fujitsu', 9, 1
UNION ALL SELECT 
    (SELECT Id FROM CustomFieldDefinitions WHERE EntityType = 'Asset' AND FieldName = 'Brand'),
    'bryant', 'Bryant', 10, 1
UNION ALL SELECT 
    (SELECT Id FROM CustomFieldDefinitions WHERE EntityType = 'Asset' AND FieldName = 'Brand'),
    'american_standard', 'American Standard', 11, 1
UNION ALL SELECT 
    (SELECT Id FROM CustomFieldDefinitions WHERE EntityType = 'Asset' AND FieldName = 'Brand'),
    'ruud', 'Ruud', 12, 1
UNION ALL SELECT 
    (SELECT Id FROM CustomFieldDefinitions WHERE EntityType = 'Asset' AND FieldName = 'Brand'),
    'other', 'Other', 99, 1;

COMMIT;

-- =============================================
-- Verification Queries
-- =============================================
-- Run these after migration to verify success:
-- SELECT COUNT(*) FROM Companies;
-- SELECT COUNT(*) FROM AssetOwners;
-- SELECT COUNT(*) FROM CustomerCompanyRoles;
-- SELECT COUNT(*) FROM CustomFieldDefinitions;
-- SELECT COUNT(*) FROM CustomFieldChoices;
-- SELECT COUNT(*) FROM Customers WHERE FirstName IS NOT NULL;
-- SELECT FirstName, LastName, Name FROM Customers LIMIT 10;
