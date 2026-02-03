-- Migration: Add IsWarrantyClaim field to WarrantyClaims table
-- Allows tracking general repairs vs actual warranty claims
-- Apply to: SQL Server

IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('WarrantyClaims') 
    AND name = 'IsWarrantyClaim'
)
BEGIN
    ALTER TABLE WarrantyClaims 
    ADD IsWarrantyClaim BIT NOT NULL DEFAULT 1;
    
    PRINT 'Added IsWarrantyClaim column to WarrantyClaims table.';
END
GO
