-- Migration: Add TaxIncluded, ContactName, SiteName, and relationship fields
-- Date: January 26, 2026
-- Description: Comprehensive feature enhancements

-- =====================================================
-- PHASE 1: Tax Included Fields
-- =====================================================

-- Add TaxIncluded to Estimates
ALTER TABLE Estimates ADD COLUMN TaxIncluded INTEGER NOT NULL DEFAULT 0;

-- Add TaxIncluded to Invoices
ALTER TABLE Invoices ADD COLUMN TaxIncluded INTEGER NOT NULL DEFAULT 0;

-- Add TaxIncluded to Jobs
ALTER TABLE Jobs ADD COLUMN TaxIncluded INTEGER NOT NULL DEFAULT 0;

-- =====================================================
-- PHASE 2: Company Contact Enhancement
-- =====================================================

-- Add ContactName and ContactCustomerId to Companies
ALTER TABLE Companies ADD COLUMN ContactName TEXT;
ALTER TABLE Companies ADD COLUMN ContactCustomerId INTEGER REFERENCES Customers(Id);

-- =====================================================
-- PHASE 3: Site Enhancement (SiteName, CompanyId)
-- =====================================================

-- Add SiteName and LocationDescription to Sites
ALTER TABLE Sites ADD COLUMN SiteName TEXT;
ALTER TABLE Sites ADD COLUMN LocationDescription TEXT;

-- Add CompanyId to Sites (allows sites to belong to companies)
ALTER TABLE Sites ADD COLUMN CompanyId INTEGER REFERENCES Companies(Id);

-- =====================================================
-- PHASE 4: Create indexes for new relationships
-- =====================================================

-- Index for Company's contact customer lookup
CREATE INDEX IF NOT EXISTS IX_Companies_ContactCustomerId ON Companies(ContactCustomerId);

-- Index for Site's company lookup
CREATE INDEX IF NOT EXISTS IX_Sites_CompanyId ON Sites(CompanyId);

-- Index for Site name search
CREATE INDEX IF NOT EXISTS IX_Sites_SiteName ON Sites(SiteName);

-- =====================================================
-- VERIFICATION QUERIES
-- =====================================================

-- Verify Estimates has TaxIncluded
SELECT sql FROM sqlite_master WHERE type='table' AND name='Estimates';

-- Verify Invoices has TaxIncluded
SELECT sql FROM sqlite_master WHERE type='table' AND name='Invoices';

-- Verify Jobs has TaxIncluded
SELECT sql FROM sqlite_master WHERE type='table' AND name='Jobs';

-- Verify Companies has ContactName and ContactCustomerId
SELECT sql FROM sqlite_master WHERE type='table' AND name='Companies';

-- Verify Sites has SiteName, LocationDescription, CompanyId
SELECT sql FROM sqlite_master WHERE type='table' AND name='Sites';

-- =====================================================
-- NOTES
-- =====================================================
-- 
-- TaxIncluded field meaning:
--   0 (false) = Calculate tax normally (add tax to subtotal)
--   1 (true)  = Tax already included in prices (don't add tax)
--
-- Company.ContactCustomerId:
--   Optional foreign key linking to an existing Customer
--   who is the primary contact for this Company
--
-- Site.SiteName:
--   User-friendly name like "Main Office", "Rental #1"
--   Helps distinguish multiple sites under same customer/company
--
-- Site.CompanyId:
--   Optional foreign key linking Site to a Company
--   Allows commercial properties to be linked to Companies
