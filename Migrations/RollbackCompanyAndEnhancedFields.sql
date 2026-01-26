-- =============================================
-- Field Enhancement Rollback Script
-- Version: 1.0
-- Date: 2025-01-26
-- Description: Rolls back field enhancement changes
-- WARNING: This will delete all Company, AssetOwner, 
--          CustomerCompanyRole, and CustomFieldChoice data!
-- =============================================

BEGIN TRANSACTION;

-- =============================================
-- STEP 1: Remove Foreign Keys (Drop Columns)
-- =============================================

-- SQLite doesn't support DROP COLUMN directly
-- You'll need to recreate tables without these columns
-- For now, we'll set them to NULL

UPDATE Customers SET CompanyId = NULL WHERE CompanyId IS NOT NULL;
UPDATE Assets SET CompanyId = NULL WHERE CompanyId IS NOT NULL;

-- =============================================
-- STEP 2: Drop New Tables
-- =============================================

DROP TABLE IF EXISTS CustomFieldChoices;
DROP TABLE IF EXISTS CustomFieldDefinitions;
DROP TABLE IF EXISTS CustomerCompanyRoles;
DROP TABLE IF EXISTS AssetOwners;
DROP TABLE IF EXISTS Companies;

-- =============================================
-- STEP 3: Restore Customer Name Field
-- =============================================

-- Reconstruct Name from FirstName + LastName
UPDATE Customers 
SET Name = CASE 
    WHEN FirstName IS NOT NULL AND LastName IS NOT NULL 
    THEN FirstName || ' ' || LastName
    WHEN FirstName IS NOT NULL 
    THEN FirstName
    WHEN LastName IS NOT NULL 
    THEN LastName
    ELSE Name
END;

-- =============================================
-- STEP 4: Remove New Columns (SQLite Limitation)
-- =============================================

-- SQLite doesn't support ALTER TABLE DROP COLUMN
-- To fully remove columns, you need to:
-- 1. Create new table without the columns
-- 2. Copy data
-- 3. Drop old table
-- 4. Rename new table

-- For Customer table
CREATE TABLE Customers_Backup AS SELECT 
    Id, CustomerNumber, Name, CompanyName, CustomerType, Status,
    Email, Phone, PreferredContact, SecondaryPhone, SecondaryEmail,
    EmergencyContact, EmergencyPhone, BillingEmail, PaymentTerms,
    CreditLimit, AccountBalance, LifetimeRevenue, LastServiceDate,
    AcquisitionSource, AcquisitionDate, LeadSource, LeadSourceDetail,
    PreferredServiceWindowStart, PreferredServiceWindowEnd, Notes,
    CreatedAt, UpdatedAt, IsActive
FROM Customers;

DROP TABLE Customers;

ALTER TABLE Customers_Backup RENAME TO Customers;

-- Recreate indexes (adjust based on your actual indexes)
CREATE INDEX IX_Customers_Name ON Customers(Name);
CREATE INDEX IX_Customers_Email ON Customers(Email);
CREATE INDEX IX_Customers_Status ON Customers(Status);

-- For Asset table
CREATE TABLE Assets_Backup AS SELECT 
    Id, CustomerId, SiteId, AssetTag, Serial, Brand, Model, Nickname,
    EquipmentType, FuelType, UnitConfig, BtuRating, TonnageX10,
    SeerRating, Seer2Rating, HspfRating, Hspf2Rating, EerRating, Eer2Rating,
    AfueRating, RefrigerantType, RefrigerantCharge, IsVariableSpeed,
    ManufactureDate, InstallDate, InstalledBy, WarrantyStartDate,
    WarrantyTermYears, PartsWarrantyYears, LaborWarrantyYears,
    CompressorWarrantyYears, HasExtendedWarranty, ExtendedWarrantyEnd,
    WarrantyNotes, PurchasePrice, EstimatedReplacementCost, Status,
    RetirementDate, RetirementReason, Notes, PhotoUrl, ManualUrl,
    LastServiceDate, NextServiceDue, ServiceHistoryCount,
    HasServiceContract, CreatedAt, UpdatedAt
FROM Assets;

DROP TABLE Assets;

ALTER TABLE Assets_Backup RENAME TO Assets;

-- Recreate indexes
CREATE INDEX IX_Assets_Serial ON Assets(Serial);
CREATE INDEX IX_Assets_CustomerId ON Assets(CustomerId);
CREATE INDEX IX_Assets_SiteId ON Assets(SiteId);

COMMIT;

-- =============================================
-- Verification
-- =============================================
-- SELECT COUNT(*) FROM Customers WHERE Name IS NOT NULL;
-- SELECT * FROM sqlite_master WHERE type='table';
