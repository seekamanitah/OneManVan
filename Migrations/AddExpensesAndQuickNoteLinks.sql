-- Add Expenses table and QuickNote entity linking
-- Migration for tracking business expenses and linking quick notes to entities
-- Run this after the main schema is in place

-- =====================================================
-- EXPENSES TABLE
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Expenses')
BEGIN
    CREATE TABLE Expenses (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ExpenseNumber NVARCHAR(20) NULL,
        ExpenseDate DATE NOT NULL DEFAULT GETDATE(),
        Category INT NOT NULL DEFAULT 99,  -- ExpenseCategory enum
        Description NVARCHAR(200) NOT NULL,
        Amount DECIMAL(10,2) NOT NULL DEFAULT 0,
        TaxAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
        TotalAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
        VendorName NVARCHAR(200) NULL,
        ReceiptNumber NVARCHAR(100) NULL,
        PaymentMethod INT NOT NULL DEFAULT 1,  -- PaymentMethod enum
        IsBillable BIT NOT NULL DEFAULT 0,
        IsReimbursed BIT NOT NULL DEFAULT 0,
        ReimbursedDate DATE NULL,
        JobId INT NULL,
        CustomerId INT NULL,
        InvoiceId INT NULL,
        EmployeeId INT NULL,
        ReceiptPath NVARCHAR(500) NULL,
        Notes NVARCHAR(1000) NULL,
        Status INT NOT NULL DEFAULT 1,  -- ExpenseStatus enum
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT FK_Expenses_Jobs FOREIGN KEY (JobId) REFERENCES Jobs(Id) ON DELETE SET NULL,
        CONSTRAINT FK_Expenses_Customers FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE SET NULL,
        CONSTRAINT FK_Expenses_Invoices FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id) ON DELETE SET NULL,
        CONSTRAINT FK_Expenses_Employees FOREIGN KEY (EmployeeId) REFERENCES Employees(Id) ON DELETE SET NULL
    );
    
    PRINT 'Created Expenses table';
END
GO

-- Index for expense queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Expenses_ExpenseDate')
BEGIN
    CREATE INDEX IX_Expenses_ExpenseDate ON Expenses(ExpenseDate DESC);
    PRINT 'Created IX_Expenses_ExpenseDate index';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Expenses_Category')
BEGIN
    CREATE INDEX IX_Expenses_Category ON Expenses(Category);
    PRINT 'Created IX_Expenses_Category index';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Expenses_JobId')
BEGIN
    CREATE INDEX IX_Expenses_JobId ON Expenses(JobId) WHERE JobId IS NOT NULL;
    PRINT 'Created IX_Expenses_JobId index';
END
GO

-- =====================================================
-- QUICKNOTES - Add Entity Linking
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('QuickNotes') AND name = 'CustomerId')
BEGIN
    ALTER TABLE QuickNotes ADD CustomerId INT NULL;
    PRINT 'Added CustomerId to QuickNotes';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('QuickNotes') AND name = 'SiteId')
BEGIN
    ALTER TABLE QuickNotes ADD SiteId INT NULL;
    PRINT 'Added SiteId to QuickNotes';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('QuickNotes') AND name = 'JobId')
BEGIN
    ALTER TABLE QuickNotes ADD JobId INT NULL;
    PRINT 'Added JobId to QuickNotes';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('QuickNotes') AND name = 'AssetId')
BEGIN
    ALTER TABLE QuickNotes ADD AssetId INT NULL;
    PRINT 'Added AssetId to QuickNotes';
END
GO

-- Add foreign keys for QuickNotes entity linking
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_QuickNotes_Customers')
BEGIN
    ALTER TABLE QuickNotes ADD CONSTRAINT FK_QuickNotes_Customers 
        FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE SET NULL;
    PRINT 'Added FK_QuickNotes_Customers';
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_QuickNotes_Sites')
BEGIN
    ALTER TABLE QuickNotes ADD CONSTRAINT FK_QuickNotes_Sites 
        FOREIGN KEY (SiteId) REFERENCES Sites(Id) ON DELETE SET NULL;
    PRINT 'Added FK_QuickNotes_Sites';
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_QuickNotes_Jobs')
BEGIN
    ALTER TABLE QuickNotes ADD CONSTRAINT FK_QuickNotes_Jobs 
        FOREIGN KEY (JobId) REFERENCES Jobs(Id) ON DELETE SET NULL;
    PRINT 'Added FK_QuickNotes_Jobs';
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_QuickNotes_Assets')
BEGIN
    ALTER TABLE QuickNotes ADD CONSTRAINT FK_QuickNotes_Assets 
        FOREIGN KEY (AssetId) REFERENCES Assets(Id) ON DELETE SET NULL;
    PRINT 'Added FK_QuickNotes_Assets';
END
GO

PRINT 'Migration AddExpensesAndQuickNoteLinks completed successfully';
