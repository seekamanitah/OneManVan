-- Migration: Add Performance Indexes
-- Date: June 2025
-- Purpose: Add missing indexes identified in QA audit for query performance

-- SQL Server Version

-- Invoice date indexes for reporting
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_InvoiceDate' AND object_id = OBJECT_ID('Invoices'))
BEGIN
    CREATE INDEX IX_Invoices_InvoiceDate ON Invoices(InvoiceDate);
    PRINT 'Created index: IX_Invoices_InvoiceDate';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_DueDate' AND object_id = OBJECT_ID('Invoices'))
BEGIN
    CREATE INDEX IX_Invoices_DueDate ON Invoices(DueDate);
    PRINT 'Created index: IX_Invoices_DueDate';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_Status' AND object_id = OBJECT_ID('Invoices'))
BEGIN
    CREATE INDEX IX_Invoices_Status ON Invoices(Status);
    PRINT 'Created index: IX_Invoices_Status';
END

-- Job scheduling indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Jobs_ScheduledDate' AND object_id = OBJECT_ID('Jobs'))
BEGIN
    CREATE INDEX IX_Jobs_ScheduledDate ON Jobs(ScheduledDate);
    PRINT 'Created index: IX_Jobs_ScheduledDate';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Jobs_Status' AND object_id = OBJECT_ID('Jobs'))
BEGIN
    CREATE INDEX IX_Jobs_Status ON Jobs(Status);
    PRINT 'Created index: IX_Jobs_Status';
END

-- Employee time log indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EmployeeTimeLogs_Date' AND object_id = OBJECT_ID('EmployeeTimeLogs'))
BEGIN
    CREATE INDEX IX_EmployeeTimeLogs_Date ON EmployeeTimeLogs(Date);
    PRINT 'Created index: IX_EmployeeTimeLogs_Date';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EmployeeTimeLogs_EmployeeId_Date' AND object_id = OBJECT_ID('EmployeeTimeLogs'))
BEGIN
    CREATE INDEX IX_EmployeeTimeLogs_EmployeeId_Date ON EmployeeTimeLogs(EmployeeId, Date);
    PRINT 'Created index: IX_EmployeeTimeLogs_EmployeeId_Date';
END

-- Payment indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Payments_PaymentDate' AND object_id = OBJECT_ID('Payments'))
BEGIN
    CREATE INDEX IX_Payments_PaymentDate ON Payments(PaymentDate);
    PRINT 'Created index: IX_Payments_PaymentDate';
END

-- Estimate indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Estimates_CreatedAt' AND object_id = OBJECT_ID('Estimates'))
BEGIN
    CREATE INDEX IX_Estimates_CreatedAt ON Estimates(CreatedAt);
    PRINT 'Created index: IX_Estimates_CreatedAt';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Estimates_Status' AND object_id = OBJECT_ID('Estimates'))
BEGIN
    CREATE INDEX IX_Estimates_Status ON Estimates(Status);
    PRINT 'Created index: IX_Estimates_Status';
END

PRINT 'Performance indexes migration completed';
