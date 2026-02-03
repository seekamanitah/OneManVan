-- Add Expenses table and QuickNote entity linking (SQLite version)
-- Migration for tracking business expenses and linking quick notes to entities

-- =====================================================
-- EXPENSES TABLE
-- =====================================================
CREATE TABLE IF NOT EXISTS Expenses (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ExpenseNumber TEXT NULL,
    ExpenseDate TEXT NOT NULL DEFAULT (date('now')),
    Category INTEGER NOT NULL DEFAULT 99,
    Description TEXT NOT NULL,
    Amount REAL NOT NULL DEFAULT 0,
    TaxAmount REAL NOT NULL DEFAULT 0,
    TotalAmount REAL NOT NULL DEFAULT 0,
    VendorName TEXT NULL,
    ReceiptNumber TEXT NULL,
    PaymentMethod INTEGER NOT NULL DEFAULT 1,
    IsBillable INTEGER NOT NULL DEFAULT 0,
    IsReimbursed INTEGER NOT NULL DEFAULT 0,
    ReimbursedDate TEXT NULL,
    JobId INTEGER NULL REFERENCES Jobs(Id) ON DELETE SET NULL,
    CustomerId INTEGER NULL REFERENCES Customers(Id) ON DELETE SET NULL,
    InvoiceId INTEGER NULL REFERENCES Invoices(Id) ON DELETE SET NULL,
    EmployeeId INTEGER NULL REFERENCES Employees(Id) ON DELETE SET NULL,
    ReceiptPath TEXT NULL,
    Notes TEXT NULL,
    Status INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    UpdatedAt TEXT NOT NULL DEFAULT (datetime('now'))
);

-- Indexes for Expenses
CREATE INDEX IF NOT EXISTS IX_Expenses_ExpenseDate ON Expenses(ExpenseDate DESC);
CREATE INDEX IF NOT EXISTS IX_Expenses_Category ON Expenses(Category);
CREATE INDEX IF NOT EXISTS IX_Expenses_JobId ON Expenses(JobId);
CREATE INDEX IF NOT EXISTS IX_Expenses_CustomerId ON Expenses(CustomerId);

-- =====================================================
-- QUICKNOTES - Add Entity Linking Columns
-- =====================================================
-- SQLite doesn't support ALTER TABLE ADD COLUMN IF NOT EXISTS, so we use PRAGMA

-- Check and add CustomerId
ALTER TABLE QuickNotes ADD COLUMN CustomerId INTEGER NULL REFERENCES Customers(Id) ON DELETE SET NULL;

-- Check and add SiteId  
ALTER TABLE QuickNotes ADD COLUMN SiteId INTEGER NULL REFERENCES Sites(Id) ON DELETE SET NULL;

-- Check and add JobId
ALTER TABLE QuickNotes ADD COLUMN JobId INTEGER NULL REFERENCES Jobs(Id) ON DELETE SET NULL;

-- Check and add AssetId
ALTER TABLE QuickNotes ADD COLUMN AssetId INTEGER NULL REFERENCES Assets(Id) ON DELETE SET NULL;

-- Indexes for QuickNotes entity links
CREATE INDEX IF NOT EXISTS IX_QuickNotes_CustomerId ON QuickNotes(CustomerId);
CREATE INDEX IF NOT EXISTS IX_QuickNotes_SiteId ON QuickNotes(SiteId);
CREATE INDEX IF NOT EXISTS IX_QuickNotes_JobId ON QuickNotes(JobId);
CREATE INDEX IF NOT EXISTS IX_QuickNotes_AssetId ON QuickNotes(AssetId);
