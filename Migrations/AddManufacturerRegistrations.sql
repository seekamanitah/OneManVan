-- =============================================
-- Manufacturer Registration URLs Migration
-- Version: 1.0
-- Date: 2025-01-26
-- Description: Creates ManufacturerRegistrations table
--              and seeds with all major HVAC brands
-- =============================================

BEGIN TRANSACTION;

-- =============================================
-- STEP 1: Create ManufacturerRegistrations Table
-- =============================================

CREATE TABLE IF NOT EXISTS ManufacturerRegistrations (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    BrandName TEXT NOT NULL,
    RegistrationUrl TEXT NULL,
    SupportPhone TEXT NULL,
    SupportEmail TEXT NULL,
    ManufacturerWebsite TEXT NULL,
    RegistrationNotes TEXT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    DisplayOrder INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    UpdatedAt TEXT NULL
);

CREATE UNIQUE INDEX IX_ManufacturerRegistrations_BrandName ON ManufacturerRegistrations(BrandName);
CREATE INDEX IX_ManufacturerRegistrations_IsActive ON ManufacturerRegistrations(IsActive);
CREATE INDEX IX_ManufacturerRegistrations_DisplayOrder ON ManufacturerRegistrations(DisplayOrder);

-- =============================================
-- STEP 2: Seed Major HVAC Brands
-- =============================================

INSERT INTO ManufacturerRegistrations (BrandName, RegistrationUrl, RegistrationNotes, IsActive, DisplayOrder) VALUES
('American Standard', 'https://www.americanstandardair.com/resources/warranty-and-registration', 'Register within 60-90 days for extended warranty', 1, 10),
('Amana', 'https://www.amana-hac.com/product-registration', 'Lifetime limited warranty on compressor when registered', 1, 20),
('Bosch', 'https://www.bosch-homecomfort.com/us/en/residential/service-support/product-warranty-registration-information', 'Register within 90 days of installation', 1, 30),
('Bryant', 'https://productregistration.bryant.com/', 'Extended warranty available with registration', 1, 40),
('Carrier', 'https://productregistration.carrier.com/Public/RegistrationForm?brand=carrier', 'Register for extended parts warranty', 1, 50),
('Daikin', 'https://daikincomfort.com/my-daikin-systems/product-registration', '12-year parts limited warranty when registered', 1, 60),
('Fujitsu', 'https://www.fujitsugeneral.com/us/support/index.html', 'See product registration section on support page', 1, 70),
('Goodman', 'https://www.goodmanmfg.com/product-registration', 'Register within 60 days for lifetime warranty', 1, 80),
('Heil', 'https://www.heil-hvac.com/en/us/product-registration-warranty', 'Extended warranty with registration', 1, 90),
('Lennox', 'https://www.lennox.com/residential/owners/register-and-review/product-registration', 'Register for extended warranty coverage', 1, 100),
('Mitsubishi Electric', 'https://www.registermehvac.com/', 'Register Mitsubishi Electric Trane HVAC US systems', 1, 110),
('Payne', 'https://www.payne.com/en/us/registration-warranty', 'Extended parts warranty with registration', 1, 120),
('Rheem', 'https://rheem.registermyunit.com/', 'Register within 60 days of installation', 1, 130),
('Ruud', 'https://ruud.registermyunit.com/', 'Register for extended warranty coverage', 1, 140),
('Trane', 'https://www.trane.com/residential/en/resources/warranty-and-registration', 'Register within 60 days for extended warranty', 1, 150),
('York', 'https://www.york.com/residential-equipment/warranty-and-registration', '10-year parts limited warranty when registered', 1, 160);

-- =============================================
-- STEP 3: Add Additional Common Brands
-- =============================================

-- Add more brands without registration URLs (can be added later)
INSERT INTO ManufacturerRegistrations (BrandName, IsActive, DisplayOrder) VALUES
('Coleman', 1, 170),
('Frigidaire', 1, 180),
('GE', 1, 190),
('Gibson', 1, 200),
('ICP', 1, 210),
('Maytag', 1, 220),
('Nordyne', 1, 230),
('Tempstar', 1, 240),
('Westinghouse', 1, 250),
('Other', 1, 999);

-- =============================================
-- VERIFICATION
-- =============================================

SELECT 
    COUNT(*) as TotalBrands,
    SUM(CASE WHEN RegistrationUrl IS NOT NULL THEN 1 ELSE 0 END) as BrandsWithUrls,
    'ManufacturerRegistrations seeded' as Status
FROM ManufacturerRegistrations;

COMMIT;

-- =============================================
-- ROLLBACK (if needed)
-- =============================================
-- BEGIN TRANSACTION;
-- DROP TABLE IF EXISTS ManufacturerRegistrations;
-- COMMIT;
