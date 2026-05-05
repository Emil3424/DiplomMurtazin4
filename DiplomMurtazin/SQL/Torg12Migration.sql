/* ===========================
   TORG-12 Documents
   Safe to re-run
   =========================== */

IF OBJECT_ID(N'dbo.Torg12Documents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Torg12Documents
    (
        Torg12ID          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Torg12Documents PRIMARY KEY,
        DocumentNumber    NVARCHAR(50) NOT NULL,
        DocumentDate      DATE NOT NULL,

        -- Receiver / shipment details
        ReceiverName      NVARCHAR(255) NOT NULL,
        ReceiverAddress   NVARCHAR(255) NULL,
        ReceiverInn       NVARCHAR(20) NULL,
        ReceiverKpp       NVARCHAR(20) NULL,
        Basis             NVARCHAR(255) NULL,  -- основание отпуска

        -- Who created
        CreatedByUserID   INT NULL,
        CreatedByEmployeeID INT NULL,
        CreatedDate       DATETIME2(7) NOT NULL CONSTRAINT DF_Torg12Documents_CreatedDate DEFAULT (SYSDATETIME()),

        -- Processing status
        Status            NVARCHAR(20) NOT NULL CONSTRAINT DF_Torg12Documents_Status DEFAULT (N'Draft'),

        Notes             NVARCHAR(1000) NULL
    );
END
GO

IF OBJECT_ID(N'dbo.Torg12Items', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Torg12Items
    (
        Torg12ItemID   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Torg12Items PRIMARY KEY,
        Torg12ID       INT NOT NULL,
        ProductID      INT NOT NULL,
        Quantity       INT NOT NULL,
        UnitPrice      DECIMAL(10,2) NOT NULL,
        TotalPrice     AS (CONVERT(DECIMAL(10,2), [Quantity] * [UnitPrice])) PERSISTED
    );
END
GO

/* ===========================
   Pending imported items (when product doesn't exist yet)
   Survives app restart
   =========================== */

IF OBJECT_ID(N'dbo.Torg12ImportMissingItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Torg12ImportMissingItems
    (
        MissingID        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Torg12ImportMissingItems PRIMARY KEY,
        Torg12ID         INT NOT NULL,
        TempProductName  NVARCHAR(255) NOT NULL,
        TempBarcode      NVARCHAR(50) NULL,
        Quantity         INT NOT NULL,
        UnitPrice        DECIMAL(10,2) NOT NULL,
        CreatedProductID INT NULL,
        Status           NVARCHAR(20) NOT NULL CONSTRAINT DF_Torg12ImportMissingItems_Status DEFAULT (N'Pending'),
        CreatedDate      DATETIME2(7) NOT NULL CONSTRAINT DF_Torg12ImportMissingItems_CreatedDate DEFAULT (SYSDATETIME())
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Torg12ImportMissingItems_Torg12Documents')
BEGIN
    ALTER TABLE dbo.Torg12ImportMissingItems WITH CHECK
    ADD CONSTRAINT FK_Torg12ImportMissingItems_Torg12Documents
        FOREIGN KEY (Torg12ID) REFERENCES dbo.Torg12Documents(Torg12ID)
        ON DELETE CASCADE;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Torg12ImportMissingItems_Products')
BEGIN
    ALTER TABLE dbo.Torg12ImportMissingItems WITH CHECK
    ADD CONSTRAINT FK_Torg12ImportMissingItems_Products
        FOREIGN KEY (CreatedProductID) REFERENCES dbo.Products(ProductID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Torg12ImportMissingItems_Status' AND object_id = OBJECT_ID(N'dbo.Torg12ImportMissingItems'))
    CREATE INDEX IX_Torg12ImportMissingItems_Status ON dbo.Torg12ImportMissingItems(Status);
GO

/* ---------- Foreign keys ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Torg12Items_Torg12Documents')
BEGIN
    ALTER TABLE dbo.Torg12Items WITH CHECK
    ADD CONSTRAINT FK_Torg12Items_Torg12Documents
        FOREIGN KEY (Torg12ID) REFERENCES dbo.Torg12Documents(Torg12ID)
        ON DELETE CASCADE;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Torg12Items_Products')
BEGIN
    ALTER TABLE dbo.Torg12Items WITH CHECK
    ADD CONSTRAINT FK_Torg12Items_Products
        FOREIGN KEY (ProductID) REFERENCES dbo.Products(ProductID);
END
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Torg12Documents_Users')
BEGIN
    ALTER TABLE dbo.Torg12Documents WITH CHECK
    ADD CONSTRAINT FK_Torg12Documents_Users
        FOREIGN KEY (CreatedByUserID) REFERENCES dbo.Users(UserID);
END
GO

IF OBJECT_ID(N'dbo.Employees', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Torg12Documents_Employees')
BEGIN
    ALTER TABLE dbo.Torg12Documents WITH CHECK
    ADD CONSTRAINT FK_Torg12Documents_Employees
        FOREIGN KEY (CreatedByEmployeeID) REFERENCES dbo.Employees(EmployeeID);
END
GO

/* ---------- Indexes ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Torg12Documents_DocumentDate' AND object_id = OBJECT_ID(N'dbo.Torg12Documents'))
    CREATE INDEX IX_Torg12Documents_DocumentDate ON dbo.Torg12Documents(DocumentDate DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Torg12Documents_DocumentNumber' AND object_id = OBJECT_ID(N'dbo.Torg12Documents'))
    CREATE INDEX IX_Torg12Documents_DocumentNumber ON dbo.Torg12Documents(DocumentNumber);
GO

