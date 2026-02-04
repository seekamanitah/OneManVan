-- =====================================================
-- Add Job Worker Time Tracking Migration (SQL Server)
-- Version: 2024.06
-- Description: Creates JobWorkers table and adds fields
--              for enhanced time tracking, invoice pricing,
--              and expense vendor linking
-- =====================================================

-- =====================================================
-- 1. Create JobWorkers Table
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'JobWorkers') AND type = N'U')
BEGIN
    CREATE TABLE JobWorkers (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        
        -- Foreign Keys
        JobId INT NOT NULL,
        EmployeeId INT NOT NULL,
        
        -- Assignment Info
        Role NVARCHAR(100) NULL,
        HourlyRateOverride DECIMAL(18,2) NULL,
        AssignedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        -- Time Tracking State
        IsClockedIn BIT NOT NULL DEFAULT 0,
        ActiveTimeEntryId INT NULL,
        
        -- Aggregated Totals (updated on clock out)
        TotalHoursWorked DECIMAL(18,2) NOT NULL DEFAULT 0,
        TotalPayEarned DECIMAL(18,2) NOT NULL DEFAULT 0,
        
        -- Timestamps
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        
        -- Constraints
        CONSTRAINT FK_JobWorkers_Jobs FOREIGN KEY (JobId) REFERENCES Jobs(Id) ON DELETE CASCADE,
        CONSTRAINT FK_JobWorkers_Employees FOREIGN KEY (EmployeeId) REFERENCES Employees(Id) ON DELETE CASCADE,
        CONSTRAINT FK_JobWorkers_TimeEntries FOREIGN KEY (ActiveTimeEntryId) REFERENCES TimeEntries(Id),
        CONSTRAINT UQ_JobWorkers_JobEmployee UNIQUE (JobId, EmployeeId)
    );
    
    PRINT 'Created JobWorkers table';
END
GO

-- Create indexes for JobWorkers
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_JobWorkers_JobId')
    CREATE INDEX IX_JobWorkers_JobId ON JobWorkers(JobId);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_JobWorkers_EmployeeId')
    CREATE INDEX IX_JobWorkers_EmployeeId ON JobWorkers(EmployeeId);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_JobWorkers_IsClockedIn')
    CREATE INDEX IX_JobWorkers_IsClockedIn ON JobWorkers(IsClockedIn) WHERE IsClockedIn = 1;
GO

-- =====================================================
-- 2. Add EmployeeId to TimeEntries
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'TimeEntries') AND name = 'EmployeeId')
BEGIN
    ALTER TABLE TimeEntries ADD EmployeeId INT NULL;
    PRINT 'Added EmployeeId to TimeEntries';
END
GO

-- Add foreign key constraint
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_TimeEntries_Employees')
BEGIN
    ALTER TABLE TimeEntries 
    ADD CONSTRAINT FK_TimeEntries_Employees 
    FOREIGN KEY (EmployeeId) REFERENCES Employees(Id);
    PRINT 'Added FK_TimeEntries_Employees constraint';
END
GO

-- Create index for EmployeeId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TimeEntries_EmployeeId')
    CREATE INDEX IX_TimeEntries_EmployeeId ON TimeEntries(EmployeeId);
GO

-- =====================================================
-- 3. Add Invoice Pricing Type Fields
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Invoices') AND name = 'PricingType')
BEGIN
    ALTER TABLE Invoices ADD PricingType INT NOT NULL DEFAULT 0;
    PRINT 'Added PricingType to Invoices (0=MaterialAndLabor, 1=FlatRate, 2=TimeAndMaterials)';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Invoices') AND name = 'FlatRateAmount')
BEGIN
    ALTER TABLE Invoices ADD FlatRateAmount DECIMAL(18,2) NULL;
    PRINT 'Added FlatRateAmount to Invoices';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Invoices') AND name = 'FlatRateDescription')
BEGIN
    ALTER TABLE Invoices ADD FlatRateDescription NVARCHAR(MAX) NULL;
    PRINT 'Added FlatRateDescription to Invoices';
END
GO

-- =====================================================
-- 4. Add Expense VendorCompanyId
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Expenses') AND name = 'VendorCompanyId')
BEGIN
    ALTER TABLE Expenses ADD VendorCompanyId INT NULL;
    PRINT 'Added VendorCompanyId to Expenses';
END
GO

-- Add foreign key constraint
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Expenses_Companies')
BEGIN
    ALTER TABLE Expenses 
    ADD CONSTRAINT FK_Expenses_Companies 
    FOREIGN KEY (VendorCompanyId) REFERENCES Companies(Id);
    PRINT 'Added FK_Expenses_Companies constraint';
END
GO

-- Create index for VendorCompanyId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Expenses_VendorCompanyId')
    CREATE INDEX IX_Expenses_VendorCompanyId ON Expenses(VendorCompanyId);
GO

PRINT 'Migration complete: Job Worker Time Tracking';
GO
