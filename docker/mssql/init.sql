-- MSSQL initialization script
-- Runs on first container startup to create the database and schema

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ClinicDb')
BEGIN
    CREATE DATABASE ClinicDb;
END
GO

USE ClinicDb;
GO

-- Create Wolverine schema for durable messaging (outbox/inbox)
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'wolverine')
BEGIN
    EXEC('CREATE SCHEMA wolverine');
END
GO

PRINT 'Database ClinicDb initialized successfully';
GO
