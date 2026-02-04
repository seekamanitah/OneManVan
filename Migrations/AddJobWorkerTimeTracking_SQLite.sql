-- =====================================================
-- Add Job Worker Time Tracking Migration (SQLite)
-- Version: 2024.06
-- Description: Creates JobWorkers table and adds fields
--              for enhanced time tracking, invoice pricing,
--              and expense vendor linking
-- =====================================================

-- =====================================================
-- 1. Create JobWorkers Table
-- =====================================================
CREATE TABLE IF NOT EXISTS JobWorkers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    
    -- Foreign Keys
    JobId INTEGER NOT NULL,
    EmployeeId INTEGER NOT NULL,
    
    -- Assignment Info
    Role TEXT,
    HourlyRateOverride REAL,
    AssignedAt TEXT NOT NULL DEFAULT (datetime('now')),
    
    -- Time Tracking State
    IsClockedIn INTEGER NOT NULL DEFAULT 0,
    ActiveTimeEntryId INTEGER,
    
    -- Aggregated Totals (updated on clock out)
    TotalHoursWorked REAL NOT NULL DEFAULT 0,
    TotalPayEarned REAL NOT NULL DEFAULT 0,
    
    -- Timestamps
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    UpdatedAt TEXT,
    
    -- Foreign Key Constraints
    FOREIGN KEY (JobId) REFERENCES Jobs(Id) ON DELETE CASCADE,
    FOREIGN KEY (EmployeeId) REFERENCES Employees(Id) ON DELETE CASCADE,
    FOREIGN KEY (ActiveTimeEntryId) REFERENCES TimeEntries(Id),
    
    -- Unique constraint for job-employee pair
    UNIQUE (JobId, EmployeeId)
);

-- Create indexes for JobWorkers
CREATE INDEX IF NOT EXISTS IX_JobWorkers_JobId ON JobWorkers(JobId);
CREATE INDEX IF NOT EXISTS IX_JobWorkers_EmployeeId ON JobWorkers(EmployeeId);
CREATE INDEX IF NOT EXISTS IX_JobWorkers_IsClockedIn ON JobWorkers(IsClockedIn);

-- =====================================================
-- 2. Add EmployeeId to TimeEntries (if not exists)
-- =====================================================
-- SQLite doesn't support IF NOT EXISTS for columns, so we use pragma
-- Check if column exists first via application code or ignore error

-- Add EmployeeId column (will fail silently if already exists)
ALTER TABLE TimeEntries ADD COLUMN EmployeeId INTEGER REFERENCES Employees(Id);

-- Create index for EmployeeId
CREATE INDEX IF NOT EXISTS IX_TimeEntries_EmployeeId ON TimeEntries(EmployeeId);

-- =====================================================
-- 3. Add Invoice Pricing Type Fields
-- =====================================================
-- PricingType: 0=MaterialAndLabor, 1=FlatRate, 2=TimeAndMaterials
ALTER TABLE Invoices ADD COLUMN PricingType INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Invoices ADD COLUMN FlatRateAmount REAL;
ALTER TABLE Invoices ADD COLUMN FlatRateDescription TEXT;

-- =====================================================
-- 4. Add Expense VendorCompanyId
-- =====================================================
ALTER TABLE Expenses ADD COLUMN VendorCompanyId INTEGER REFERENCES Companies(Id);

-- Create index for VendorCompanyId
CREATE INDEX IF NOT EXISTS IX_Expenses_VendorCompanyId ON Expenses(VendorCompanyId);
