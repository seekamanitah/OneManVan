-- =====================================================
-- Database Cleanup and Enhancement Migration (SQLite)
-- Version: 2024.01
-- Description: SQLite version of the migration script
-- =====================================================

-- Note: SQLite doesn't support IF NOT EXISTS for columns
-- This script is idempotent - safe to run multiple times

-- =====================================================
-- 1. Add indexes for Dashboard KPIs (if not exist)
-- =====================================================

CREATE INDEX IF NOT EXISTS IX_Invoices_InvoiceDate ON Invoices(InvoiceDate);
CREATE INDEX IF NOT EXISTS IX_Invoices_Status_InvoiceDate ON Invoices(Status, InvoiceDate);
CREATE INDEX IF NOT EXISTS IX_Invoices_DueDate ON Invoices(DueDate);
CREATE INDEX IF NOT EXISTS IX_Payments_PaymentDate ON Payments(PaymentDate);
CREATE INDEX IF NOT EXISTS IX_Payments_InvoiceId ON Payments(InvoiceId);
CREATE INDEX IF NOT EXISTS IX_Jobs_ScheduledDate ON Jobs(ScheduledDate);
CREATE INDEX IF NOT EXISTS IX_Jobs_Status_ScheduledDate ON Jobs(Status, ScheduledDate);
CREATE INDEX IF NOT EXISTS IX_Jobs_CompletedAt ON Jobs(CompletedAt);
CREATE INDEX IF NOT EXISTS IX_Estimates_CreatedAt ON Estimates(CreatedAt);
CREATE INDEX IF NOT EXISTS IX_Estimates_Status ON Estimates(Status);

-- =====================================================
-- 2. Create DashboardMetricsCache table
-- =====================================================

CREATE TABLE IF NOT EXISTS DashboardMetricsCache (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MetricKey TEXT NOT NULL UNIQUE,
    MetricValue TEXT NOT NULL,
    CalculatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    ExpiresAt TEXT NOT NULL
);

-- =====================================================
-- 3. Create RouteOptimizations table
-- =====================================================

CREATE TABLE IF NOT EXISTS RouteOptimizations (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    OptimizationDate TEXT NOT NULL,
    TotalJobs INTEGER NOT NULL DEFAULT 0,
    TotalDistanceMiles REAL,
    TotalDurationMinutes INTEGER,
    OptimizedRoute TEXT,
    StartLatitude REAL,
    StartLongitude REAL,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS IX_RouteOptimizations_Date ON RouteOptimizations(OptimizationDate);

-- =====================================================
-- Note: Additional columns for Sites and Jobs
-- These may need to be added via EF Core migration
-- if they don't exist in the model
-- =====================================================

-- SQLite doesn't support ALTER TABLE ADD COLUMN IF NOT EXISTS
-- The following columns should be verified in the model:
-- Sites: Latitude REAL, Longitude REAL
-- Jobs: GoogleCalendarEventId TEXT, LastSyncedAt TEXT, EstimatedDurationMinutes INTEGER
