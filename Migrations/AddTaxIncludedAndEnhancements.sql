-- AddTaxIncludedAndEnhancements.sql (SQL Server Compatible)
-- This migration is for SQL Server (not SQLite)
-- For SQLite, use Entity Framework migrations or different syntax

-- Add TaxIncluded columns (use IF NOT EXISTS properly)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Estimates') AND name = 'TaxIncluded')
BEGIN
    ALTER TABLE Estimates ADD TaxIncluded BIT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Invoices') AND name = 'TaxIncluded')
BEGIN
    ALTER TABLE Invoices ADD TaxIncluded BIT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Jobs') AND name = 'TaxIncluded')
BEGIN
    ALTER TABLE Jobs ADD TaxIncluded BIT NOT NULL DEFAULT 0;
END
GO

-- Add Company contact fields
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Companies') AND name = 'ContactName')
BEGIN
    ALTER TABLE Companies ADD ContactName NVARCHAR(MAX) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Companies') AND name = 'ContactCustomerId')
BEGIN
    ALTER TABLE Companies ADD ContactCustomerId INT NULL;
    ALTER TABLE Companies ADD CONSTRAINT FK_Companies_Customers FOREIGN KEY (ContactCustomerId) REFERENCES Customers(Id);
END
GO

-- Add Site fields
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sites') AND name = 'SiteName')
BEGIN
    ALTER TABLE Sites ADD SiteName NVARCHAR(MAX) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sites') AND name = 'LocationDescription')
BEGIN
    ALTER TABLE Sites ADD LocationDescription NVARCHAR(MAX) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sites') AND name = 'CompanyId')
BEGIN
    ALTER TABLE Sites ADD CompanyId INT NULL;
    ALTER TABLE Sites ADD CONSTRAINT FK_Sites_Companies FOREIGN KEY (CompanyId) REFERENCES Companies(Id);
END
GO

-- Add indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Companies_ContactCustomerId')
BEGIN
    CREATE INDEX IX_Companies_ContactCustomerId ON Companies(ContactCustomerId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sites_CompanyId')
BEGIN
    CREATE INDEX IX_Sites_CompanyId ON Sites(CompanyId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sites_SiteName')
BEGIN
    CREATE INDEX IX_Sites_SiteName ON Sites(SiteName);
END
GO
