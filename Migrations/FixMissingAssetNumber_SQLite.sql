-- Fix missing AssetNumber column in Assets table
-- Run this against your SQLite database

-- Add AssetNumber column if it doesn't exist
ALTER TABLE Assets ADD COLUMN AssetNumber TEXT;

-- Verify the column was added
SELECT name FROM pragma_table_info('Assets') WHERE name = 'AssetNumber';
