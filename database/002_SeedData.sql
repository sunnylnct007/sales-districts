SET IDENTITY_INSERT dbo.Salesperson ON;
INSERT INTO dbo.Salesperson (SalespersonId, Name) VALUES
    (1, N'Anna Jensen'),
    (2, N'Bo Nielsen'),
    (3, N'Carl Hansen'),
    (4, N'Dorte Pedersen'),
    (5, N'Erik Larsen'),
    (6, N'Freja Sørensen'),
    (7, N'Gustav Madsen');
SET IDENTITY_INSERT dbo.Salesperson OFF;

SET IDENTITY_INSERT dbo.District ON;
INSERT INTO dbo.District (DistrictId, Name, PrimarySalespersonId) VALUES
    (1, N'North Denmark',    1),
    (2, N'Central Denmark',  5),
    (3, N'Southern Denmark', 1),
    (4, N'Zealand',          6),
    (5, N'Capital Region',   3);
SET IDENTITY_INSERT dbo.District OFF;

INSERT INTO dbo.DistrictSalesperson (DistrictId, SalespersonId, Role) VALUES
    (1, 2, N'Secondary'),
    (2, 4, N'Secondary'),
    (5, 2, N'Secondary'),
    (5, 4, N'Secondary');

SET IDENTITY_INSERT dbo.Store ON;
INSERT INTO dbo.Store (StoreId, Name, DistrictId) VALUES
    (1,  N'Aalborg Store',    1),
    (2,  N'Hjørring Store',   1),
    (3,  N'Aarhus Store',     2),
    (4,  N'Randers Store',    2),
    (5,  N'Odense Store',     3),
    (6,  N'Esbjerg Store',    3),
    (7,  N'Roskilde Store',   4),
    (8,  N'Næstved Store',    4),
    (9,  N'Copenhagen Store', 5),
    (10, N'Hillerød Store',   5);
SET IDENTITY_INSERT dbo.Store OFF;
