-- =============================================
-- Add HomeAddress to Customer Table
-- Migration Date: 2025-01-XX
-- Description: Adds HomeAddress field to support navigation and service location tracking
-- =============================================

USE OneManVanDB;
GO

-- Check if column exists before adding
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Customers') 
    AND name = 'HomeAddress'
)
BEGIN
    PRINT 'Adding HomeAddress column to Customers table...';
    
    ALTER TABLE dbo.Customers
    ADD HomeAddress NVARCHAR(500) NULL;
    
    PRINT 'HomeAddress column added successfully.';
END
ELSE
BEGIN
    PRINT 'HomeAddress column already exists.';
END
GO

-- Optional: Add an index for address searches if needed
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'IX_Customers_HomeAddress' 
    AND object_id = OBJECT_ID('dbo.Customers')
)
BEGIN
    PRINT 'Creating index on HomeAddress...';
    
    CREATE NONCLUSTERED INDEX IX_Customers_HomeAddress
    ON dbo.Customers(HomeAddress)
    WHERE HomeAddress IS NOT NULL;
    
    PRINT 'Index created successfully.';
END
GO

-- Verify the column was added
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Customers' 
    AND COLUMN_NAME = 'HomeAddress';
GO

PRINT 'Migration completed successfully!';
GO
