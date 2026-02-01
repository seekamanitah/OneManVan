-- =====================================================
-- Add CompanySettings Table Migration
-- Version: 2024.02
-- Description: Creates the CompanySettings table for 
--              company branding and document settings
-- =====================================================

-- Create CompanySettings table if it doesn't exist
CREATE TABLE IF NOT EXISTS CompanySettings (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CompanyName TEXT NOT NULL DEFAULT 'OneManVan',
    Tagline TEXT,
    Address TEXT,
    City TEXT,
    State TEXT,
    ZipCode TEXT,
    Email TEXT,
    Phone TEXT,
    Website TEXT,
    LogoBase64 TEXT,
    LogoFileName TEXT,
    TaxId TEXT,
    LicenseNumber TEXT,
    InsuranceNumber TEXT,
    PaymentTerms TEXT DEFAULT 'Payment due within 30 days of invoice date.',
    DocumentFooter TEXT DEFAULT 'Thank you for your business!',
    BankDetails TEXT,
    GoogleCalendarSettings TEXT,
    MapsApiKey TEXT,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    UpdatedAt TEXT NOT NULL DEFAULT (datetime('now'))
);

-- Insert default settings if table is empty
INSERT OR IGNORE INTO CompanySettings (Id, CompanyName, PaymentTerms, DocumentFooter)
SELECT 1, 'OneManVan', 'Payment due within 30 days of invoice date.', 'Thank you for your business!'
WHERE NOT EXISTS (SELECT 1 FROM CompanySettings WHERE Id = 1);

-- Verify the table was created
SELECT 'CompanySettings table created successfully' AS Status;
