-- ============================================================================
-- Precast Elements Estimator — SQL Server schema
--
-- Adds shared, multi-user storage for the three things that today only live
-- in each browser's localStorage: Cost Settings, Projects/Tenders, and the
-- Element Groups (Walls/Columns) added to each project's summary.
--
-- Run this once against the target database. Safe to re-run: every CREATE is
-- guarded so it only creates what's missing.
-- ============================================================================

-- ---------------------------------------------------------------------------
-- CostSetting: flat key/value table mirroring the DEFAULT_COSTS dictionary in
-- index.html. Key/value (rather than one column per rate) means adding a new
-- rate later needs a new row, not a schema change.
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CostSetting' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.CostSetting (
        SettingKey      NVARCHAR(100)   NOT NULL PRIMARY KEY,
        SettingValue    DECIMAL(18,4)   NOT NULL,
        ModifiedBy      NVARCHAR(256)   NULL,
        ModifiedAtUtc   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

-- ---------------------------------------------------------------------------
-- Project: a Project/Tender. Replaces the single projName/projCode pair that
-- used to be the only "project" concept in the app.
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Project' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.Project (
        Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        Name            NVARCHAR(200)   NOT NULL,
        Code            NVARCHAR(100)   NULL,
        ClientName      NVARCHAR(200)   NULL,
        Status          NVARCHAR(50)    NULL,
        CreatedBy       NVARCHAR(256)   NULL,
        CreatedAtUtc    DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        ModifiedBy      NVARCHAR(256)   NULL,
        ModifiedAtUtc   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

-- ---------------------------------------------------------------------------
-- ElementGroup: one row per entry in the app's "Project Summary" (a Walls or
-- Columns group added via wAddToSummary/colAddToSummary). PricePerM3/Total
-- are pulled out as real columns for listing/reporting; DataJson carries the
-- full object the client already builds today (costPerUnit, qtyPerUnit,
-- unitRates, qtyUnits, raw, rawSections, rawCustomElements, rawEoItems,
-- rawConsumables, inputs, rates) so the client can restore a form exactly
-- like it does today from localStorage, without normalizing every nested
-- shape into its own table.
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ElementGroup' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.ElementGroup (
        Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ProjectId       UNIQUEIDENTIFIER NOT NULL,
        GroupId         NVARCHAR(200)   NOT NULL,
        ElementType     NVARCHAR(20)    NOT NULL CHECK (ElementType IN ('Walls','Columns')),
        PricePerM3      DECIMAL(18,4)   NULL,
        Total           DECIMAL(18,2)   NULL,
        DataJson        NVARCHAR(MAX)   NOT NULL,
        CreatedBy       NVARCHAR(256)   NULL,
        CreatedAtUtc    DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        ModifiedBy      NVARCHAR(256)   NULL,
        ModifiedAtUtc   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_ElementGroup_Project FOREIGN KEY (ProjectId)
            REFERENCES dbo.Project (Id) ON DELETE CASCADE,
        CONSTRAINT CK_ElementGroup_DataJson_IsJson CHECK (ISJSON(DataJson) = 1)
    );

    CREATE INDEX IX_ElementGroup_ProjectId ON dbo.ElementGroup (ProjectId);
END
GO

-- ---------------------------------------------------------------------------
-- Seed CostSetting with the current DEFAULT_COSTS values from index.html so
-- the app has sane rates the first time it reads from the database.
-- ---------------------------------------------------------------------------
MERGE dbo.CostSetting AS target
USING (VALUES
    ('Concrete 50 MPa', 265),
    ('Concrete 65 MPa', 295),
    ('Concrete 50 MPa (Special Mix)', 1000),
    ('RL1018', 3.2), ('RL1118', 3.2), ('RL1218', 3.2), ('RL718', 3.2), ('RL818', 3.2), ('RL918', 3.2),
    ('SL102', 2.75), ('SL62', 3.04), ('SL72', 2.69), ('SL81', 2.72), ('SL82', 2.59), ('SL92', 3.16),
    ('Steel Bars', 3.2), ('Concrete Testing', 40), ('Ripbox', 90), ('Lifting', 25),
    ('Wages', 45), ('Shopdrawings', 20), ('Formwork', 35), ('Patching', 15),
    ('Ferrule with chair', 5), ('Threadbar', 13), ('Couplers', 11), ('Special Accessories', 5)
) AS source (SettingKey, SettingValue)
ON target.SettingKey = source.SettingKey
WHEN NOT MATCHED BY TARGET THEN
    INSERT (SettingKey, SettingValue, ModifiedAtUtc) VALUES (source.SettingKey, source.SettingValue, SYSUTCDATETIME());
GO
