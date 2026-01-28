-- TradeFlow FSM Database Initialization
-- This script runs when the Docker container starts for the first time

-- Create TradeFlow database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'TradeFlowFSM')
BEGIN
    CREATE DATABASE TradeFlowFSM
    COLLATE SQL_Latin1_General_CP1_CI_AS;
    PRINT 'Database TradeFlowFSM created successfully';
END
ELSE
BEGIN
    PRINT 'Database TradeFlowFSM already exists';
END
GO

USE TradeFlowFSM;
GO

-- Enable snapshot isolation for better concurrency during sync operations
ALTER DATABASE TradeFlowFSM SET ALLOW_SNAPSHOT_ISOLATION ON;
ALTER DATABASE TradeFlowFSM SET READ_COMMITTED_SNAPSHOT ON;
PRINT 'Snapshot isolation enabled';
GO

-- Create sync user for mobile apps (less privileged than sa)
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'tradeflow_sync')
BEGIN
    CREATE LOGIN tradeflow_sync WITH PASSWORD = 'SyncUser2025!', CHECK_POLICY = OFF;
    PRINT 'Login tradeflow_sync created';
END
ELSE
BEGIN
    PRINT 'Login tradeflow_sync already exists';
END
GO

USE TradeFlowFSM;
GO

IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'tradeflow_sync')
BEGIN
    CREATE USER tradeflow_sync FOR LOGIN tradeflow_sync;
    ALTER ROLE db_datareader ADD MEMBER tradeflow_sync;
    ALTER ROLE db_datawriter ADD MEMBER tradeflow_sync;
    PRINT 'User tradeflow_sync created and granted permissions';
END
ELSE
BEGIN
    PRINT 'User tradeflow_sync already exists';
END
GO

-- Grant execute permissions for sync procedures (if any are added later)
GRANT EXECUTE TO tradeflow_sync;
GO

PRINT '========================================';
PRINT 'TradeFlow FSM Database Initialized';
PRINT 'Database: TradeFlowFSM';
PRINT 'SA Account: sa';
PRINT 'Sync Account: tradeflow_sync';
PRINT 'Ready for Entity Framework migrations';
PRINT '========================================';
GO
