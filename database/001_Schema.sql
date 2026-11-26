CREATE TABLE dbo.Salesperson (
    SalespersonId INT IDENTITY CONSTRAINT PK_Salesperson PRIMARY KEY,
    Name          NVARCHAR(200) NOT NULL CONSTRAINT CK_Salesperson_Name CHECK (LTRIM(Name) <> N'')
);

CREATE TABLE dbo.District (
    DistrictId           INT IDENTITY CONSTRAINT PK_District PRIMARY KEY,
    Name                 NVARCHAR(100) NOT NULL
                         CONSTRAINT UQ_District_Name UNIQUE
                         CONSTRAINT CK_District_Name CHECK (LTRIM(Name) <> N''),
    -- Not unique: a salesperson can be primary for several districts.
    PrimarySalespersonId INT NOT NULL
                         CONSTRAINT FK_District_PrimarySalesperson REFERENCES dbo.Salesperson (SalespersonId)
);

CREATE INDEX IX_District_PrimarySalespersonId ON dbo.District (PrimarySalespersonId);

CREATE TABLE dbo.Store (
    StoreId    INT IDENTITY CONSTRAINT PK_Store PRIMARY KEY,
    Name       NVARCHAR(200) NOT NULL CONSTRAINT CK_Store_Name CHECK (LTRIM(Name) <> N''),
    DistrictId INT NOT NULL CONSTRAINT FK_Store_District REFERENCES dbo.District (DistrictId)
);

CREATE INDEX IX_Store_DistrictId ON dbo.Store (DistrictId);

-- Non-primary roles. The primary is District.PrimarySalespersonId.
CREATE TABLE dbo.DistrictSalesperson (
    DistrictId    INT NOT NULL CONSTRAINT FK_DistrictSalesperson_District
                  REFERENCES dbo.District (DistrictId) ON DELETE CASCADE,
    SalespersonId INT NOT NULL CONSTRAINT FK_DistrictSalesperson_Salesperson
                  REFERENCES dbo.Salesperson (SalespersonId),
    Role          NVARCHAR(20) NOT NULL CONSTRAINT CK_DistrictSalesperson_Role CHECK (Role IN (N'Secondary')),

    CONSTRAINT PK_DistrictSalesperson PRIMARY KEY (DistrictId, SalespersonId)
);

CREATE INDEX IX_DistrictSalesperson_SalespersonId ON dbo.DistrictSalesperson (SalespersonId);
GO

-- The primary can't also hold another role in the same district.
-- UPDLOCK, HOLDLOCK: concurrent transactions can't both pass the check.
CREATE TRIGGER dbo.TR_DistrictSalesperson_NotPrimary ON dbo.DistrictSalesperson
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1
               FROM inserted i
               JOIN dbo.District d WITH (UPDLOCK, HOLDLOCK) ON d.DistrictId = i.DistrictId
               WHERE d.PrimarySalespersonId = i.SalespersonId)
        THROW 50001, N'Salesperson is already the primary of this district', 1;
END;
GO

CREATE TRIGGER dbo.TR_District_PrimaryHasNoOtherRole ON dbo.District
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE(PrimarySalespersonId)
       AND EXISTS (SELECT 1
                   FROM inserted i
                   JOIN dbo.DistrictSalesperson ds WITH (UPDLOCK, HOLDLOCK)
                     ON ds.DistrictId = i.DistrictId AND ds.SalespersonId = i.PrimarySalespersonId)
        THROW 50002, N'Salesperson already has a role in this district', 1;
END;
