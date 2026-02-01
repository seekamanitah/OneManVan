-- Migration: Add Invoice References to EmployeeTimeLog (SQLite version)
-- This enables automatic time log creation from invoices

-- SQLite doesn't support ALTER TABLE ADD COLUMN with constraints in one statement
-- We need to recreate the table with new columns

-- Step 1: Create new table with updated schema
CREATE TABLE EmployeeTimeLogs_New (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EmployeeId INTEGER NOT NULL,
    JobId INTEGER NULL,
    CustomerId INTEGER NULL,
    InvoiceId INTEGER NULL,
    InvoiceLineItemId INTEGER NULL,
    Date TEXT NOT NULL,
    ClockIn TEXT NULL,
    ClockOut TEXT NULL,
    HoursWorked REAL NOT NULL,
    HourlyRate REAL NOT NULL DEFAULT 0,
    TotalPay REAL NOT NULL DEFAULT 0,
    IsOvertime INTEGER NOT NULL DEFAULT 0,
    StartTime TEXT NULL,
    EndTime TEXT NULL,
    BreakMinutes INTEGER NOT NULL DEFAULT 0,
    Description TEXT NULL,
    Notes TEXT NULL,
    Source TEXT NULL,
    IsApproved INTEGER NOT NULL DEFAULT 0,
    ApprovedBy TEXT NULL,
    ApprovedAt TEXT NULL,
    IsPaid INTEGER NOT NULL DEFAULT 0,
    PaymentId INTEGER NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NULL,
    FOREIGN KEY (EmployeeId) REFERENCES Employees(Id) ON DELETE CASCADE,
    FOREIGN KEY (JobId) REFERENCES Jobs(Id) ON DELETE SET NULL,
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE SET NULL,
    FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id) ON DELETE SET NULL,
    FOREIGN KEY (InvoiceLineItemId) REFERENCES InvoiceLineItems(Id) ON DELETE SET NULL,
    FOREIGN KEY (PaymentId) REFERENCES EmployeePayments(Id) ON DELETE SET NULL
);

-- Step 2: Copy data from old table
INSERT INTO EmployeeTimeLogs_New (
    Id, EmployeeId, JobId, Date, HoursWorked, IsOvertime, StartTime, EndTime,
    BreakMinutes, Notes, IsApproved, ApprovedBy, ApprovedAt, IsPaid, PaymentId, CreatedAt
)
SELECT 
    Id, EmployeeId, JobId, Date, HoursWorked, IsOvertime, StartTime, EndTime,
    BreakMinutes, Notes, IsApproved, ApprovedBy, ApprovedAt, IsPaid, PaymentId, CreatedAt
FROM EmployeeTimeLogs;

-- Step 3: Drop old table
DROP TABLE EmployeeTimeLogs;

-- Step 4: Rename new table
ALTER TABLE EmployeeTimeLogs_New RENAME TO EmployeeTimeLogs;

-- Step 5: Create indexes
CREATE INDEX IX_EmployeeTimeLogs_EmployeeId ON EmployeeTimeLogs(EmployeeId);
CREATE INDEX IX_EmployeeTimeLogs_JobId ON EmployeeTimeLogs(JobId);
CREATE INDEX IX_EmployeeTimeLogs_CustomerId ON EmployeeTimeLogs(CustomerId);
CREATE INDEX IX_EmployeeTimeLogs_InvoiceId ON EmployeeTimeLogs(InvoiceId);
CREATE INDEX IX_EmployeeTimeLogs_InvoiceLineItemId ON EmployeeTimeLogs(InvoiceLineItemId);
CREATE INDEX IX_EmployeeTimeLogs_Date ON EmployeeTimeLogs(Date);
CREATE INDEX IX_EmployeeTimeLogs_Source ON EmployeeTimeLogs(Source);

SELECT 'Migration completed: Employee TimeLog invoice references added (SQLite)';
