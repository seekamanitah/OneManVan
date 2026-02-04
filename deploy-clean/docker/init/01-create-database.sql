-- Clean initialization: create application databases only.
-- Do NOT create logins/users here to avoid hard-coded passwords.

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'OneManVanDB')
BEGIN
    CREATE DATABASE OneManVanDB COLLATE SQL_Latin1_General_CP1_CI_AS;
END
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'OneManVanIdentity')
BEGIN
    CREATE DATABASE OneManVanIdentity COLLATE SQL_Latin1_General_CP1_CI_AS;
END
GO

ALTER DATABASE OneManVanDB SET ALLOW_SNAPSHOT_ISOLATION ON;
ALTER DATABASE OneManVanDB SET READ_COMMITTED_SNAPSHOT ON;
GO
