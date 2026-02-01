-- Migration: Add Performance Indexes (SQLite Version)
-- Date: June 2025
-- Purpose: Add missing indexes identified in QA audit for query performance

-- Invoice date indexes for reporting
CREATE INDEX IF NOT EXISTS IX_Invoices_InvoiceDate ON Invoices(InvoiceDate);
CREATE INDEX IF NOT EXISTS IX_Invoices_DueDate ON Invoices(DueDate);
CREATE INDEX IF NOT EXISTS IX_Invoices_Status ON Invoices(Status);

-- Job scheduling indexes
CREATE INDEX IF NOT EXISTS IX_Jobs_ScheduledDate ON Jobs(ScheduledDate);
CREATE INDEX IF NOT EXISTS IX_Jobs_Status ON Jobs(Status);

-- Employee time log indexes
CREATE INDEX IF NOT EXISTS IX_EmployeeTimeLogs_Date ON EmployeeTimeLogs(Date);
CREATE INDEX IF NOT EXISTS IX_EmployeeTimeLogs_EmployeeId_Date ON EmployeeTimeLogs(EmployeeId, Date);

-- Payment indexes
CREATE INDEX IF NOT EXISTS IX_Payments_PaymentDate ON Payments(PaymentDate);

-- Estimate indexes
CREATE INDEX IF NOT EXISTS IX_Estimates_CreatedAt ON Estimates(CreatedAt);
CREATE INDEX IF NOT EXISTS IX_Estimates_Status ON Estimates(Status);

SELECT 'Performance indexes migration completed (SQLite)';
