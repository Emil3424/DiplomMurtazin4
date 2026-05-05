IF OBJECT_ID(N'dbo.AuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AuditLog]
    (
        [AuditID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [EventDate] DATETIME2(7) NOT NULL CONSTRAINT [DF_AuditLog_EventDate] DEFAULT (SYSDATETIME()),
        [UserLogin] NVARCHAR(50) NULL,
        [EmployeeID] INT NULL,
        [ActionType] NVARCHAR(50) NOT NULL,
        [EntityType] NVARCHAR(100) NOT NULL,
        [EntityID] NVARCHAR(100) NULL,
        [Details] NVARCHAR(1000) NULL,
        [Metadata] NVARCHAR(2000) NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLog_EventDate' AND object_id = OBJECT_ID(N'dbo.AuditLog'))
BEGIN
    CREATE INDEX [IX_AuditLog_EventDate] ON [dbo].[AuditLog]([EventDate] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLog_ActionType' AND object_id = OBJECT_ID(N'dbo.AuditLog'))
BEGIN
    CREATE INDEX [IX_AuditLog_ActionType] ON [dbo].[AuditLog]([ActionType]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLog_EntityType' AND object_id = OBJECT_ID(N'dbo.AuditLog'))
BEGIN
    CREATE INDEX [IX_AuditLog_EntityType] ON [dbo].[AuditLog]([EntityType]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AuditLog_Employees')
BEGIN
    ALTER TABLE [dbo].[AuditLog]
    ADD CONSTRAINT [FK_AuditLog_Employees]
        FOREIGN KEY ([EmployeeID]) REFERENCES [dbo].[Employees]([EmployeeID]);
END
GO
