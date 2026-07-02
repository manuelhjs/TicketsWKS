/* =====================================================================
   Datos semilla mínimos para poder ejecutar el módulo.
   Los códigos de usuario (RequesterUserCode, etc.) son referencias
   externas a SAP; aquí se usan valores de ejemplo.
   ===================================================================== */
USE B1_PROA_MX_V2;
GO

/* Catálogo de estatus (Ids estables usados por el enum TicketStatus en C#) */
MERGE dbo.TicketStatuses AS target
USING (VALUES
    (1, N'A',  N'Creado'),
    (2, N'EP', N'En Proceso'),
    (3, N'C',  N'Cerrado')
) AS src (Id, Code, Name)
ON target.Id = src.Id
WHEN MATCHED THEN UPDATE SET Code = src.Code, Name = src.Name
WHEN NOT MATCHED THEN INSERT (Id, Code, Name) VALUES (src.Id, src.Code, src.Name);
GO

/* Áreas */
IF NOT EXISTS (SELECT 1 FROM dbo.Areas)
BEGIN
    INSERT INTO dbo.Areas (Code, Name) VALUES
        (N'CAL', N'Calidad'),
        (N'PD',  N'Producción'),
        (N'SIS', N'Sistemas');
END
GO

/* Tipos de solicitud por área */
IF NOT EXISTS (SELECT 1 FROM dbo.TicketTypes)
BEGIN
    DECLARE @cal INT = (SELECT Id FROM dbo.Areas WHERE Code = N'CAL');
    DECLARE @pd  INT = (SELECT Id FROM dbo.Areas WHERE Code = N'PD');
    DECLARE @sis INT = (SELECT Id FROM dbo.Areas WHERE Code = N'SIS');

    INSERT INTO dbo.TicketTypes (AreaId, Name, DefaultResponsibleUserCode) VALUES
        (@cal, N'No conformidad',      N'jhernandez'),
        (@cal, N'Devolución',          N'jhernandez'),
        (@pd,  N'Falla de máquina',    N'klopez'),
        (@pd,  N'Mantenimiento',       N'klopez'),
        (@sis, N'Soporte de sistemas', N'blozano'),
        (@sis, N'Alta de usuario',     N'blozano');
END
GO

/* Un par de tickets de ejemplo */
IF NOT EXISTS (SELECT 1 FROM dbo.Tickets)
BEGIN
    DECLARE @tType INT = (SELECT TOP 1 Id FROM dbo.TicketTypes ORDER BY Id);

    INSERT INTO dbo.Tickets
        (TicketTypeId, StatusId, RequesterUserCode, DepartmentCode, ResponsibleUserCode,
         Description, Category, RegisteredTime, CreatedAt)
    VALUES
        (@tType, 1, N'jperez', N'VEN', N'jhernandez',
         N'Ticket de ejemplo — revisión de no conformidad.', N'S', CAST(SYSUTCDATETIME() AS TIME(0)), SYSUTCDATETIME());
END
GO
