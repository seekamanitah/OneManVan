-- Migration: Add IsWarrantyClaim field to WarrantyClaims table
-- Allows tracking general repairs vs actual warranty claims
-- Apply to: SQLite

ALTER TABLE WarrantyClaims ADD COLUMN IsWarrantyClaim INTEGER NOT NULL DEFAULT 1;
