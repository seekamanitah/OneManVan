-- =============================================
-- Invoice Line Items Migration
-- Version: 1.0
-- Date: 2025-01-26
-- Description: Creates InvoiceLineItems table for
--              detailed invoice itemization
-- =============================================

BEGIN TRANSACTION;

-- =============================================
-- STEP 1: Create InvoiceLineItems Table
-- =============================================

CREATE TABLE IF NOT EXISTS InvoiceLineItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    InvoiceId INTEGER NOT NULL,
    Source TEXT NOT NULL DEFAULT 'Custom',
    SourceId INTEGER NULL,
    Description TEXT NOT NULL,
    Quantity REAL NOT NULL DEFAULT 1.0,
    UnitPrice REAL NOT NULL DEFAULT 0.0,
    SerialNumber TEXT NULL,
    CreatedAssetId INTEGER NULL,
    Notes TEXT NULL,
    DisplayOrder INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id) ON DELETE CASCADE,
    FOREIGN KEY (CreatedAssetId) REFERENCES Assets(Id) ON DELETE SET NULL
);

CREATE INDEX IX_InvoiceLineItems_InvoiceId ON InvoiceLineItems(InvoiceId);
CREATE INDEX IX_InvoiceLineItems_Source ON InvoiceLineItems(Source);
CREATE INDEX IX_InvoiceLineItems_SerialNumber ON InvoiceLineItems(SerialNumber);
CREATE INDEX IX_InvoiceLineItems_CreatedAssetId ON InvoiceLineItems(CreatedAssetId);

-- =============================================
-- STEP 2: Migrate Existing Invoice Data (Optional)
-- =============================================

-- If you have existing invoices, create line items from aggregate amounts
-- This is optional - only run if you want to preserve existing data

-- Create labor line items
INSERT INTO InvoiceLineItems (InvoiceId, Source, Description, Quantity, UnitPrice, DisplayOrder)
SELECT 
    Id,
    'Labor',
    'Labor charges',
    1.0,
    LaborAmount,
    1
FROM Invoices
WHERE LaborAmount > 0;

-- Create parts line items
INSERT INTO InvoiceLineItems (InvoiceId, Source, Description, Quantity, UnitPrice, DisplayOrder)
SELECT 
    Id,
    'Custom',
    'Parts and materials',
    1.0,
    PartsAmount,
    2
FROM Invoices
WHERE PartsAmount > 0;

-- Create other charges line items
INSERT INTO InvoiceLineItems (InvoiceId, Source, Description, Quantity, UnitPrice, DisplayOrder)
SELECT 
    Id,
    'Custom',
    'Other charges',
    1.0,
    OtherAmount,
    3
FROM Invoices
WHERE OtherAmount > 0;

-- =============================================
-- VERIFICATION
-- =============================================

SELECT 
    COUNT(*) as TotalLineItems,
    COUNT(DISTINCT InvoiceId) as InvoicesWithLineItems,
    'InvoiceLineItems created' as Status
FROM InvoiceLineItems;

COMMIT;

-- =============================================
-- ROLLBACK (if needed)
-- =============================================
-- BEGIN TRANSACTION;
-- DROP TABLE IF EXISTS InvoiceLineItems;
-- COMMIT;
