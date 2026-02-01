-- Migration: Add Invoice References to EmployeeTimeLog
-- This enables automatic time log creation from invoices

-- Add new columns to EmployeeTimeLog table
ALTER TABLE EmployeeTimeLogs ADD CustomerId INT NULL;
ALTER TABLE EmployeeTimeLogs ADD InvoiceId INT NULL;
ALTER TABLE EmployeeTimeLogs ADD InvoiceLineItemId INT NULL;
ALTER TABLE EmployeeTimeLogs ADD ClockIn DATETIME2 NULL;
ALTER TABLE EmployeeTimeLogs ADD ClockOut DATETIME2 NULL;
ALTER TABLE EmployeeTimeLogs ADD HourlyRate DECIMAL(10,2) NOT NULL DEFAULT 0;
ALTER TABLE EmployeeTimeLogs ADD TotalPay DECIMAL(10,2) NOT NULL DEFAULT 0;
ALTER TABLE EmployeeTimeLogs ADD Description NVARCHAR(500) NULL;
ALTER TABLE EmployeeTimeLogs ADD Source NVARCHAR(50) NULL;
ALTER TABLE EmployeeTimeLogs ADD UpdatedAt DATETIME2 NULL;

-- Add foreign key constraints
ALTER TABLE EmployeeTimeLogs
ADD CONSTRAINT FK_EmployeeTimeLogs_Customers_CustomerId
FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
ON DELETE NO ACTION;

ALTER TABLE EmployeeTimeLogs
ADD CONSTRAINT FK_EmployeeTimeLogs_Invoices_InvoiceId
FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id)
ON DELETE NO ACTION;

ALTER TABLE EmployeeTimeLogs
ADD CONSTRAINT FK_EmployeeTimeLogs_InvoiceLineItems_InvoiceLineItemId
FOREIGN KEY (InvoiceLineItemId) REFERENCES InvoiceLineItems(Id)
ON DELETE NO ACTION;

-- Add indexes for performance
CREATE INDEX IX_EmployeeTimeLogs_CustomerId ON EmployeeTimeLogs(CustomerId);
CREATE INDEX IX_EmployeeTimeLogs_InvoiceId ON EmployeeTimeLogs(InvoiceId);
CREATE INDEX IX_EmployeeTimeLogs_InvoiceLineItemId ON EmployeeTimeLogs(InvoiceLineItemId);
CREATE INDEX IX_EmployeeTimeLogs_Source ON EmployeeTimeLogs(Source);

PRINT 'Migration completed: Employee TimeLog invoice references added';
