-- OneManVan Database Initialization
-- This script runs when the Docker container starts for the first time

-- Create OneManVan database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'OneManVanDB')
BEGIN
    CREATE DATABASE OneManVanDB
    COLLATE SQL_Latin1_General_CP1_CI_AS;
    PRINT 'Database OneManVanDB created successfully';
END
ELSE
BEGIN
    PRINT 'Database OneManVanDB already exists';
END
GO

USE OneManVanDB;
GO

-- Enable snapshot isolation for better concurrency during sync operations
ALTER DATABASE OneManVanDB SET ALLOW_SNAPSHOT_ISOLATION ON;
ALTER DATABASE OneManVanDB SET READ_COMMITTED_SNAPSHOT ON;
PRINT 'Snapshot isolation enabled';
GO

-- Create sync user for mobile apps (less privileged than sa)
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'onemanvan_sync')
BEGIN
    CREATE LOGIN onemanvan_sync WITH PASSWORD = 'SyncUser2025!', CHECK_POLICY = OFF;
    PRINT 'Login onemanvan_sync created';
END
ELSE
BEGIN
    PRINT 'Login onemanvan_sync already exists';
END
GO

USE OneManVanDB;
GO

IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'onemanvan_sync')
BEGIN
    CREATE USER onemanvan_sync FOR LOGIN onemanvan_sync;
    ALTER ROLE db_datareader ADD MEMBER onemanvan_sync;
    ALTER ROLE db_datawriter ADD MEMBER onemanvan_sync;
    PRINT 'User onemanvan_sync created and granted permissions';
END
ELSE
BEGIN
    PRINT 'User onemanvan_sync already exists';
END
GO

-- Grant execute permissions for sync procedures (if any are added later)
GRANT EXECUTE TO onemanvan_sync;
GO

PRINT '========================================';
PRINT 'OneManVan Database Initialized';
PRINT 'Database: OneManVanDB';
PRINT 'SA Account: sa';
PRINT 'Sync Account: onemanvan_sync';
PRINT 'Ready for Entity Framework migrations';
PRINT '========================================';
GO
