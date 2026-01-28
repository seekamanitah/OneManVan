-- Add only NEW columns (tables already exist)
-- Run this on your database

-- Add TaxIncluded to Estimates (if not exists)
ALTER TABLE Estimates ADD COLUMN TaxIncluded INTEGER NOT NULL DEFAULT 0;

-- Add TaxIncluded to Invoices (if not exists)
ALTER TABLE Invoices ADD COLUMN TaxIncluded INTEGER NOT NULL DEFAULT 0;

-- Add TaxIncluded to Jobs (if not exists)
ALTER TABLE Jobs ADD COLUMN TaxIncluded INTEGER NOT NULL DEFAULT 0;

-- Add ContactName to Companies (if not exists)
ALTER TABLE Companies ADD COLUMN ContactName TEXT;

-- Add ContactCustomerId to Companies (if not exists)
ALTER TABLE Companies ADD COLUMN ContactCustomerId INTEGER REFERENCES Customers(Id);

-- Add SiteName to Sites (if not exists)
ALTER TABLE Sites ADD COLUMN SiteName TEXT;

-- Add LocationDescription to Sites (if not exists)
ALTER TABLE Sites ADD COLUMN LocationDescription TEXT;

-- Add CompanyId to Sites (if not exists)
ALTER TABLE Sites ADD COLUMN CompanyId INTEGER REFERENCES Companies(Id);

-- Add indexes
CREATE INDEX IF NOT EXISTS IX_Companies_ContactCustomerId ON Companies(ContactCustomerId);
CREATE INDEX IF NOT EXISTS IX_Sites_CompanyId ON Sites(CompanyId);
CREATE INDEX IF NOT EXISTS IX_Sites_SiteName ON Sites(SiteName);
