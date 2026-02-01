-- =====================================================
-- Database Cleanup and Enhancement Migration
-- Version: 2024.01
-- Description: Adds indexes for performance, ensures
--              data integrity, and prepares for new features
-- =====================================================

-- =====================================================
-- 1. Add missing indexes for Dashboard KPIs
-- =====================================================

-- Invoice indexes for revenue reporting
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_InvoiceDate')
    CREATE INDEX IX_Invoices_InvoiceDate ON Invoices(InvoiceDate);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_Status_InvoiceDate')
    CREATE INDEX IX_Invoices_Status_InvoiceDate ON Invoices(Status, InvoiceDate);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_DueDate')
    CREATE INDEX IX_Invoices_DueDate ON Invoices(DueDate);

-- Payment indexes for revenue tracking
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Payments_PaymentDate')
    CREATE INDEX IX_Payments_PaymentDate ON Payments(PaymentDate);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Payments_InvoiceId')
    CREATE INDEX IX_Payments_InvoiceId ON Payments(InvoiceId);

-- Job indexes for scheduling and optimization
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Jobs_ScheduledDate')
    CREATE INDEX IX_Jobs_ScheduledDate ON Jobs(ScheduledDate);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Jobs_Status_ScheduledDate')
    CREATE INDEX IX_Jobs_Status_ScheduledDate ON Jobs(Status, ScheduledDate);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Jobs_CompletedAt')
    CREATE INDEX IX_Jobs_CompletedAt ON Jobs(CompletedAt);

-- Estimate indexes for sales tracking
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Estimates_CreatedAt')
    CREATE INDEX IX_Estimates_CreatedAt ON Estimates(CreatedAt);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Estimates_Status')
    CREATE INDEX IX_Estimates_Status ON Estimates(Status);

-- =====================================================
-- 2. Ensure Site has GPS coordinates for route optimization
-- =====================================================

-- Latitude column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sites') AND name = 'Latitude')
BEGIN
    ALTER TABLE Sites ADD Latitude DECIMAL(10,7) NULL;
    PRINT 'Added Latitude column to Sites';
END

-- Longitude column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sites') AND name = 'Longitude')
BEGIN
    ALTER TABLE Sites ADD Longitude DECIMAL(10,7) NULL;
    PRINT 'Added Longitude column to Sites';
END

-- =====================================================
-- 3. Add Google Calendar sync columns to Jobs
-- =====================================================

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Jobs') AND name = 'GoogleCalendarEventId')
BEGIN
    ALTER TABLE Jobs ADD GoogleCalendarEventId NVARCHAR(256) NULL;
    PRINT 'Added GoogleCalendarEventId column to Jobs';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Jobs') AND name = 'LastSyncedAt')
BEGIN
    ALTER TABLE Jobs ADD LastSyncedAt DATETIME2 NULL;
    PRINT 'Added LastSyncedAt column to Jobs';
END

-- =====================================================
-- 4. Add estimated duration to Jobs for scheduling
-- =====================================================

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Jobs') AND name = 'EstimatedDurationMinutes')
BEGIN
    ALTER TABLE Jobs ADD EstimatedDurationMinutes INT NULL DEFAULT 60;
    PRINT 'Added EstimatedDurationMinutes column to Jobs';
END

-- =====================================================
-- 5. Add dashboard cache table for performance
-- =====================================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('DashboardMetricsCache') AND type = 'U')
BEGIN
    CREATE TABLE DashboardMetricsCache (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        MetricKey NVARCHAR(100) NOT NULL,
        MetricValue NVARCHAR(MAX) NOT NULL,
        CalculatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ExpiresAt DATETIME2 NOT NULL,
        
        CONSTRAINT UQ_DashboardMetricsCache_Key UNIQUE (MetricKey)
    );
    PRINT 'Created DashboardMetricsCache table';
END

-- =====================================================
-- 6. Verify CompanySettings table exists
-- =====================================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('CompanySettings') AND type = 'U')
BEGIN
    PRINT 'CompanySettings table does not exist - will be created by EF Core';
END
ELSE
BEGIN
    PRINT 'CompanySettings table exists';
END

-- =====================================================
-- 7. Add Route Optimization tracking table
-- =====================================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('RouteOptimizations') AND type = 'U')
BEGIN
    CREATE TABLE RouteOptimizations (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        OptimizationDate DATE NOT NULL,
        TotalJobs INT NOT NULL DEFAULT 0,
        TotalDistanceMiles DECIMAL(10,2) NULL,
        TotalDurationMinutes INT NULL,
        OptimizedRoute NVARCHAR(MAX) NULL, -- JSON array of job IDs in order
        StartLatitude DECIMAL(10,7) NULL,
        StartLongitude DECIMAL(10,7) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    
    CREATE INDEX IX_RouteOptimizations_Date ON RouteOptimizations(OptimizationDate);
    PRINT 'Created RouteOptimizations table';
END

-- =====================================================
-- 8. Create view for Dashboard KPIs
-- =====================================================

IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_DashboardKPIs')
    DROP VIEW vw_DashboardKPIs;
GO

CREATE VIEW vw_DashboardKPIs AS
SELECT
    -- Revenue metrics
    (SELECT COALESCE(SUM(Total), 0) FROM Invoices WHERE InvoiceDate >= CAST(GETDATE() AS DATE) AND Status != 5) AS TodayRevenue,
    (SELECT COALESCE(SUM(Total), 0) FROM Invoices WHERE InvoiceDate >= DATEADD(DAY, -1, CAST(GETDATE() AS DATE)) AND InvoiceDate < CAST(GETDATE() AS DATE) AND Status != 5) AS YesterdayRevenue,
    (SELECT COALESCE(SUM(Total), 0) FROM Invoices WHERE MONTH(InvoiceDate) = MONTH(GETDATE()) AND YEAR(InvoiceDate) = YEAR(GETDATE()) AND Status != 5) AS MonthRevenue,
    (SELECT COALESCE(SUM(Total), 0) FROM Invoices WHERE MONTH(InvoiceDate) = MONTH(DATEADD(MONTH, -1, GETDATE())) AND YEAR(InvoiceDate) = YEAR(DATEADD(MONTH, -1, GETDATE())) AND Status != 5) AS LastMonthRevenue,
    
    -- Outstanding receivables
    (SELECT COALESCE(SUM(Total - PaidAmount), 0) FROM Invoices WHERE Status IN (1, 2, 3) AND DueDate >= GETDATE()) AS CurrentReceivables,
    (SELECT COALESCE(SUM(Total - PaidAmount), 0) FROM Invoices WHERE Status IN (1, 2, 3) AND DueDate < GETDATE() AND DueDate >= DATEADD(DAY, -30, GETDATE())) AS Receivables30Days,
    (SELECT COALESCE(SUM(Total - PaidAmount), 0) FROM Invoices WHERE Status IN (1, 2, 3) AND DueDate < DATEADD(DAY, -30, GETDATE()) AND DueDate >= DATEADD(DAY, -60, GETDATE())) AS Receivables60Days,
    (SELECT COALESCE(SUM(Total - PaidAmount), 0) FROM Invoices WHERE Status IN (1, 2, 3) AND DueDate < DATEADD(DAY, -60, GETDATE())) AS Receivables90PlusDays,
    
    -- Job metrics
    (SELECT COUNT(*) FROM Jobs WHERE ScheduledDate >= CAST(GETDATE() AS DATE) AND ScheduledDate < DATEADD(DAY, 1, CAST(GETDATE() AS DATE))) AS TodayJobs,
    (SELECT COUNT(*) FROM Jobs WHERE ScheduledDate >= CAST(GETDATE() AS DATE) AND ScheduledDate < DATEADD(DAY, 7, CAST(GETDATE() AS DATE))) AS WeekJobs,
    (SELECT COUNT(*) FROM Jobs WHERE CompletedAt >= DATEADD(DAY, -7, GETDATE())) AS CompletedThisWeek,
    (SELECT COUNT(*) FROM Jobs WHERE Status = 0) AS PendingJobs,
    (SELECT COUNT(*) FROM Jobs WHERE Status = 1) AS ScheduledJobs,
    (SELECT COUNT(*) FROM Jobs WHERE Status = 2) AS InProgressJobs,
    
    -- Customer metrics
    (SELECT COUNT(*) FROM Customers WHERE CreatedAt >= DATEADD(DAY, -30, GETDATE())) AS NewCustomers30Days,
    (SELECT COUNT(*) FROM Customers WHERE Status = 0) AS ActiveCustomers,
    
    -- Estimate metrics
    (SELECT COUNT(*) FROM Estimates WHERE Status = 0) AS DraftEstimates,
    (SELECT COUNT(*) FROM Estimates WHERE Status = 1) AS SentEstimates,
    (SELECT COALESCE(SUM(Total), 0) FROM Estimates WHERE Status = 1) AS PendingEstimateValue,
    
    -- Service Agreement metrics
    (SELECT COUNT(*) FROM ServiceAgreements WHERE EndDate >= GETDATE() AND EndDate < DATEADD(DAY, 30, GETDATE())) AS AgreementsExpiring30Days
GO

PRINT 'Created vw_DashboardKPIs view';

-- =====================================================
-- 9. Update statistics for optimal query performance
-- =====================================================

UPDATE STATISTICS Invoices;
UPDATE STATISTICS Jobs;
UPDATE STATISTICS Payments;
UPDATE STATISTICS Customers;
UPDATE STATISTICS Estimates;
UPDATE STATISTICS Sites;
UPDATE STATISTICS Assets;

PRINT 'Updated table statistics';

-- =====================================================
-- Summary
-- =====================================================
PRINT '';
PRINT '==============================================';
PRINT 'Database Cleanup Migration Complete';
PRINT '==============================================';
PRINT 'Added indexes for performance';
PRINT 'Added GPS columns to Sites';
PRINT 'Added Google Calendar sync columns to Jobs';
PRINT 'Created DashboardMetricsCache table';
PRINT 'Created RouteOptimizations table';
PRINT 'Created vw_DashboardKPIs view';
PRINT '==============================================';
