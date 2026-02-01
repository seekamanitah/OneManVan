-- Clean initialization: create application databases only.
-- Do NOT create logins/users here to avoid hard-coded passwords.

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'TradeFlowFSM')
BEGIN
    CREATE DATABASE TradeFlowFSM COLLATE SQL_Latin1_General_CP1_CI_AS;
END
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'TradeFlowIdentity')
BEGIN
    CREATE DATABASE TradeFlowIdentity COLLATE SQL_Latin1_General_CP1_CI_AS;
END
GO

ALTER DATABASE TradeFlowFSM SET ALLOW_SNAPSHOT_ISOLATION ON;
ALTER DATABASE TradeFlowFSM SET READ_COMMITTED_SNAPSHOT ON;
GO
