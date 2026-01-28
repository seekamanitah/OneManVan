-- =============================================
-- Product & Invoice Enhancements Migration
-- Version: 1.0
-- Date: 2025-01-26
-- Description: Adds SerialNumber to Product and
--              Registration tracking to Asset
-- =============================================

BEGIN TRANSACTION;

-- =============================================
-- STEP 1: Add SerialNumber to Products
-- =============================================

-- Check if column exists first
SELECT CASE 
    WHEN COUNT(*) > 0 THEN 'Column exists'
    ELSE 'Adding column'
END as Status
FROM pragma_table_info('Products') 
WHERE name='SerialNumber';

-- Add SerialNumber column to Products table
ALTER TABLE Products ADD COLUMN SerialNumber TEXT NULL;

-- Create index for serial number lookups
CREATE INDEX IF NOT EXISTS IX_Products_SerialNumber ON Products(SerialNumber);

-- =============================================
-- STEP 2: Add Registration Tracking to Assets
-- =============================================

-- Add IsRegisteredOnline column
ALTER TABLE Assets ADD COLUMN IsRegisteredOnline INTEGER NOT NULL DEFAULT 0;

-- Add RegistrationDate column
ALTER TABLE Assets ADD COLUMN RegistrationDate TEXT NULL;

-- Add RegistrationConfirmation column
ALTER TABLE Assets ADD COLUMN RegistrationConfirmation TEXT NULL;

-- Create index for unregistered assets queries
CREATE INDEX IF NOT EXISTS IX_Assets_IsRegisteredOnline ON Assets(IsRegisteredOnline);

-- Create index for registration date
CREATE INDEX IF NOT EXISTS IX_Assets_RegistrationDate ON Assets(RegistrationDate);

-- =============================================
-- STEP 3: Data Migration (if needed)
-- =============================================

-- Mark any assets with warranty as needing registration
-- (unless they're older than 1 year - probably already registered or expired)
UPDATE Assets 
SET IsRegisteredOnline = 0
WHERE Serial IS NOT NULL 
  AND InstallDate IS NOT NULL
  AND InstallDate > date('now', '-1 year')
  AND WarrantyStartDate IS NOT NULL;

-- =============================================
-- VERIFICATION
-- =============================================

-- Verify Products table structure
SELECT 
    name,
    type,
    'Products' as TableName
FROM pragma_table_info('Products')
WHERE name IN ('SerialNumber');

-- Verify Assets table structure
SELECT 
    name,
    type,
    'Assets' as TableName
FROM pragma_table_info('Assets')
WHERE name IN ('IsRegisteredOnline', 'RegistrationDate', 'RegistrationConfirmation');

-- Count assets that need registration
SELECT 
    COUNT(*) as UnregisteredCount,
    'Assets needing registration' as Description
FROM Assets
WHERE Serial IS NOT NULL
  AND IsRegisteredOnline = 0
  AND InstallDate IS NOT NULL
  AND InstallDate > date('now', '-1 year');

COMMIT;

-- =============================================
-- ROLLBACK (if needed)
-- =============================================
-- BEGIN TRANSACTION;
-- ALTER TABLE Products DROP COLUMN SerialNumber;
-- ALTER TABLE Assets DROP COLUMN IsRegisteredOnline;
-- ALTER TABLE Assets DROP COLUMN RegistrationDate;
-- ALTER TABLE Assets DROP COLUMN RegistrationConfirmation;
-- DROP INDEX IF EXISTS IX_Products_SerialNumber;
-- DROP INDEX IF EXISTS IX_Assets_IsRegisteredOnline;
-- DROP INDEX IF EXISTS IX_Assets_RegistrationDate;
-- COMMIT;
