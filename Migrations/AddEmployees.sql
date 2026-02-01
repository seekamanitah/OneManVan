-- =====================================================
-- Add Employees & Contractors Migration (SQLite)
-- Version: 2024.04
-- Description: Creates tables for employee/contractor 
--              management with time tracking, payments,
--              and performance notes
-- =====================================================

-- Main Employees table
CREATE TABLE IF NOT EXISTS Employees (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    
    -- Basic Info
    FirstName TEXT NOT NULL,
    LastName TEXT NOT NULL,
    Type INTEGER NOT NULL DEFAULT 0,
    Status INTEGER NOT NULL DEFAULT 0,
    StartDate TEXT NOT NULL,
    TerminationDate TEXT,
    
    -- Contact
    Address TEXT,
    City TEXT,
    State TEXT,
    ZipCode TEXT,
    Phone TEXT,
    Email TEXT,
    
    -- Emergency Contact
    EmergencyContactName TEXT,
    EmergencyContactPhone TEXT,
    EmergencyContactRelationship TEXT,
    
    -- Tax Info (should be encrypted in production)
    TaxId TEXT,
    IsEin INTEGER NOT NULL DEFAULT 0,
    
    -- Pay Info
    PayRateType INTEGER NOT NULL DEFAULT 0,
    PayRate REAL NOT NULL DEFAULT 0,
    OvertimeMultiplier REAL NOT NULL DEFAULT 1.5,
    OvertimeThresholdHours INTEGER NOT NULL DEFAULT 40,
    PaymentMethod INTEGER NOT NULL DEFAULT 0,
    
    -- PTO
    PtoBalanceHours REAL NOT NULL DEFAULT 0,
    PtoAccrualPerYear REAL NOT NULL DEFAULT 0,
    
    -- Last Payment
    LastPaidDate TEXT,
    LastPaidAmount REAL,
    LastPaidMethod TEXT,
    
    -- Subcontractor Fields
    CompanyName TEXT,
    ContactPerson TEXT,
    CompanyEin TEXT,
    ServiceProvided TEXT,
    InvoiceRequired INTEGER NOT NULL DEFAULT 0,
    InsuranceOnFile INTEGER NOT NULL DEFAULT 0,
    
    -- Documents & Compliance
    HasW4 INTEGER NOT NULL DEFAULT 0,
    W4DocumentId INTEGER,
    HasI9 INTEGER NOT NULL DEFAULT 0,
    I9DocumentId INTEGER,
    Has1099Setup INTEGER NOT NULL DEFAULT 0,
    W9DocumentId INTEGER,
    InsuranceCertDocumentId INTEGER,
    BackgroundCheckDate TEXT,
    BackgroundCheckResult TEXT,
    DrugTestDate TEXT,
    DrugTestResult TEXT,
    
    -- Notes & Photo
    Notes TEXT,
    PhotoPath TEXT,
    
    -- Tracking
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    UpdatedAt TEXT
);

-- Time Logs table
CREATE TABLE IF NOT EXISTS EmployeeTimeLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EmployeeId INTEGER NOT NULL,
    JobId INTEGER,
    Date TEXT NOT NULL,
    HoursWorked REAL NOT NULL DEFAULT 0,
    IsOvertime INTEGER NOT NULL DEFAULT 0,
    StartTime TEXT,
    EndTime TEXT,
    BreakMinutes INTEGER NOT NULL DEFAULT 0,
    Notes TEXT,
    IsApproved INTEGER NOT NULL DEFAULT 0,
    ApprovedBy TEXT,
    ApprovedAt TEXT,
    IsPaid INTEGER NOT NULL DEFAULT 0,
    PaymentId INTEGER,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    
    FOREIGN KEY (EmployeeId) REFERENCES Employees(Id) ON DELETE CASCADE,
    FOREIGN KEY (JobId) REFERENCES Jobs(Id) ON DELETE SET NULL,
    FOREIGN KEY (PaymentId) REFERENCES EmployeePayments(Id) ON DELETE SET NULL
);

-- Performance Notes table
CREATE TABLE IF NOT EXISTS EmployeePerformanceNotes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EmployeeId INTEGER NOT NULL,
    JobId INTEGER,
    Date TEXT NOT NULL,
    Category INTEGER NOT NULL DEFAULT 7,
    Rating INTEGER,
    Note TEXT NOT NULL,
    CreatedBy TEXT,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    
    FOREIGN KEY (EmployeeId) REFERENCES Employees(Id) ON DELETE CASCADE,
    FOREIGN KEY (JobId) REFERENCES Jobs(Id) ON DELETE SET NULL
);

-- Payments table
CREATE TABLE IF NOT EXISTS EmployeePayments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EmployeeId INTEGER NOT NULL,
    PayDate TEXT NOT NULL,
    GrossAmount REAL NOT NULL DEFAULT 0,
    Deductions REAL NOT NULL DEFAULT 0,
    NetAmount REAL NOT NULL DEFAULT 0,
    PaymentMethod INTEGER NOT NULL DEFAULT 0,
    PeriodStart TEXT,
    PeriodEnd TEXT,
    RegularHours REAL NOT NULL DEFAULT 0,
    OvertimeHours REAL NOT NULL DEFAULT 0,
    RegularPay REAL NOT NULL DEFAULT 0,
    OvertimePay REAL NOT NULL DEFAULT 0,
    ReferenceNumber TEXT,
    Notes TEXT,
    IsBonus INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    
    FOREIGN KEY (EmployeeId) REFERENCES Employees(Id) ON DELETE CASCADE
);

-- Indexes for efficient queries
CREATE INDEX IF NOT EXISTS IX_Employees_Status ON Employees(Status);
CREATE INDEX IF NOT EXISTS IX_Employees_Type ON Employees(Type);
CREATE INDEX IF NOT EXISTS IX_Employees_LastName ON Employees(LastName);

CREATE INDEX IF NOT EXISTS IX_EmployeeTimeLogs_EmployeeId ON EmployeeTimeLogs(EmployeeId);
CREATE INDEX IF NOT EXISTS IX_EmployeeTimeLogs_Date ON EmployeeTimeLogs(Date);
CREATE INDEX IF NOT EXISTS IX_EmployeeTimeLogs_JobId ON EmployeeTimeLogs(JobId);
CREATE INDEX IF NOT EXISTS IX_EmployeeTimeLogs_IsPaid ON EmployeeTimeLogs(IsPaid);

CREATE INDEX IF NOT EXISTS IX_EmployeePerformanceNotes_EmployeeId ON EmployeePerformanceNotes(EmployeeId);
CREATE INDEX IF NOT EXISTS IX_EmployeePerformanceNotes_Date ON EmployeePerformanceNotes(Date);

CREATE INDEX IF NOT EXISTS IX_EmployeePayments_EmployeeId ON EmployeePayments(EmployeeId);
CREATE INDEX IF NOT EXISTS IX_EmployeePayments_PayDate ON EmployeePayments(PayDate);

SELECT 'Employees & Contractors tables created successfully' AS Status;
