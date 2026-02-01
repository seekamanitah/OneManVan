-- Migration: Add Auto-Generated ID Fields (SQLite Version)
-- Date: February 2026
-- Purpose: Add AssetNumber and InventoryNumber columns for auto-ID generation

-- SQLite Version - Run these statements one at a time

-- Add AssetNumber column to Assets table
ALTER TABLE Assets ADD COLUMN AssetNumber TEXT;

-- Add InventoryNumber column to InventoryItems table
ALTER TABLE InventoryItems ADD COLUMN InventoryNumber TEXT;

-- Create index on AssetNumber for faster lookups
CREATE INDEX IF NOT EXISTS IX_Assets_AssetNumber ON Assets(AssetNumber);

-- Create index on InventoryNumber for faster lookups
CREATE INDEX IF NOT EXISTS IX_InventoryItems_InventoryNumber ON InventoryItems(InventoryNumber);

-- Optional: Backfill existing records with auto-generated IDs
-- UPDATE Assets SET AssetNumber = 'AST-' || printf('%04d', Id) WHERE AssetNumber IS NULL;
-- UPDATE InventoryItems SET InventoryNumber = 'INV-' || printf('%04d', Id) WHERE InventoryNumber IS NULL;
