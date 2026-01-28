-- Add Soft Delete Columns to Invoices
-- Date: January 27, 2026
-- Purpose: Enable soft delete with optional hard delete

-- Add soft delete tracking columns
ALTER TABLE Invoices ADD COLUMN IsDeleted INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Invoices ADD COLUMN DeletedAt TEXT;
ALTER TABLE Invoices ADD COLUMN DeletedBy TEXT;

-- Create index for performance (filtering deleted items)
CREATE INDEX IF NOT EXISTS IX_Invoices_IsDeleted ON Invoices(IsDeleted);

-- Verification query
SELECT sql FROM sqlite_master WHERE type='table' AND name='Invoices';
