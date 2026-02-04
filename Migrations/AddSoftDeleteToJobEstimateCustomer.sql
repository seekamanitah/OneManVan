-- Add Soft Delete Columns to Jobs, Estimates, and Customers
-- Date: February 4, 2026
-- Purpose: Enable soft delete with recovery capability for Jobs, Estimates, and Customers

-- ==========================================
-- Jobs Table - Add Soft Delete Columns
-- ==========================================
ALTER TABLE Jobs ADD COLUMN IsDeleted INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Jobs ADD COLUMN DeletedAt TEXT;
ALTER TABLE Jobs ADD COLUMN DeletedBy TEXT;

-- Create index for performance (filtering deleted items)
CREATE INDEX IF NOT EXISTS IX_Jobs_IsDeleted ON Jobs(IsDeleted);

-- ==========================================
-- Estimates Table - Add Soft Delete Columns
-- ==========================================
ALTER TABLE Estimates ADD COLUMN IsDeleted INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Estimates ADD COLUMN DeletedAt TEXT;
ALTER TABLE Estimates ADD COLUMN DeletedBy TEXT;

-- Create index for performance (filtering deleted items)
CREATE INDEX IF NOT EXISTS IX_Estimates_IsDeleted ON Estimates(IsDeleted);

-- ==========================================
-- Customers Table - Add Soft Delete Columns
-- ==========================================
ALTER TABLE Customers ADD COLUMN IsDeleted INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Customers ADD COLUMN DeletedAt TEXT;
ALTER TABLE Customers ADD COLUMN DeletedBy TEXT;

-- Create index for performance (filtering deleted items)
CREATE INDEX IF NOT EXISTS IX_Customers_IsDeleted ON Customers(IsDeleted);

-- ==========================================
-- Verification Queries
-- ==========================================
SELECT 'Jobs table columns:' AS Info;
SELECT sql FROM sqlite_master WHERE type='table' AND name='Jobs';

SELECT 'Estimates table columns:' AS Info;
SELECT sql FROM sqlite_master WHERE type='table' AND name='Estimates';

SELECT 'Customers table columns:' AS Info;
SELECT sql FROM sqlite_master WHERE type='table' AND name='Customers';
