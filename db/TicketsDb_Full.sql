/* =====================================================================
   Módulo de Tickets — Script COMPLETO y portable (SQL Server)
   Esquema + datos semilla. Idempotente: se puede ejecutar varias veces.

   Cómo ejecutar en otro servidor:
   - Recomendado: abrir en SSMS / Azure Data Studio y ejecutar (F5).
   - O por línea de comandos preservando UTF-8 (por los acentos):
       sqlcmd -S TU_SERVIDOR -f 65001 -i TicketsDb_Full.sql
     (con autenticación SQL agrega: -U usuario -P password)

   Las tablas se crean DENTRO de la base de SAP B1_PROA_MX_V2 (junto a las
   tablas del sistema), que es a donde apunta la cadena de conexión TicketsDb.
   Si prefieres una base independiente, reemplaza el bloque 1 por:
       IF DB_ID('TicketsDb') IS NULL CREATE DATABASE TicketsDb;
       GO
       USE TicketsDb;
       GO
   y ajusta el Initial Catalog de la cadena de conexión al mismo nombre.
   ===================================================================== */

/* ---------- 1. Base de datos destino ---------- */
USE B1_PROA_MX_V2;
GO

/* ---------- 2. Catálogo de estatus ----------
   Reemplaza los magic strings 'A' / 'EP' / 'C' del legacy.
   Los Id coinciden con el enum TicketStatus del código C#. */
IF OBJECT_ID('dbo.TicketStatuses', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TicketStatuses
    (
        Id      TINYINT       NOT NULL CONSTRAINT PK_TicketStatuses PRIMARY KEY,
        Code    NVARCHAR(10)  NOT NULL,
        Name    NVARCHAR(50)  NOT NULL,
        CONSTRAINT UQ_TicketStatuses_Code UNIQUE (Code)
    );
END
GO

/* ---------- 3. Áreas (antes VW_GP_AREAS) ---------- */
IF OBJECT_ID('dbo.Areas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Areas
    (
        Id        INT           NOT NULL IDENTITY(1,1) CONSTRAINT PK_Areas PRIMARY KEY,
        Code      NVARCHAR(20)  NOT NULL,
        Name      NVARCHAR(100) NOT NULL,
        IsActive  BIT           NOT NULL CONSTRAINT DF_Areas_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0)  NOT NULL CONSTRAINT DF_Areas_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_Areas_Code UNIQUE (Code)
    );
END
GO

/* ---------- 4. Tipos de solicitud (antes @GP_ASIGNACIONTP) ---------- */
IF OBJECT_ID('dbo.TicketTypes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TicketTypes
    (
        Id                         INT           NOT NULL IDENTITY(1,1) CONSTRAINT PK_TicketTypes PRIMARY KEY,
        AreaId                     INT           NOT NULL,
        Name                       NVARCHAR(150) NOT NULL,
        DefaultResponsibleUserCode NVARCHAR(50)  NULL,   -- referencia externa a usuario SAP
        IsActive                   BIT           NOT NULL CONSTRAINT DF_TicketTypes_IsActive DEFAULT (1),
        CreatedAt                  DATETIME2(0)  NOT NULL CONSTRAINT DF_TicketTypes_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_TicketTypes_Areas FOREIGN KEY (AreaId) REFERENCES dbo.Areas (Id)
    );
    CREATE INDEX IX_TicketTypes_AreaId ON dbo.TicketTypes (AreaId);
END
GO

/* ---------- 5. Tickets (antes @GP_TICKETS) ----------
   RequesterUserCode / ResponsibleUserCode / DepartmentCode son
   referencias externas a maestros de SAP (sin FK local, a propósito). */
IF OBJECT_ID('dbo.Tickets', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tickets
    (
        Id                  INT            NOT NULL IDENTITY(1,1) CONSTRAINT PK_Tickets PRIMARY KEY,
        TicketTypeId        INT            NOT NULL,
        StatusId            TINYINT        NOT NULL CONSTRAINT DF_Tickets_StatusId DEFAULT (1),
        RequesterUserCode   NVARCHAR(50)   NOT NULL,
        DepartmentCode      NVARCHAR(50)   NULL,
        ResponsibleUserCode NVARCHAR(50)   NULL,
        Description         NVARCHAR(MAX)  NOT NULL,
        Category            NVARCHAR(20)   NULL,          -- antes U_Tipo (p.ej. 'S')
        AttachmentFileName  NVARCHAR(260)  NULL,
        QualityDepartment   NVARCHAR(100)  NULL,          -- solo aplica a área Calidad
        Machine             NVARCHAR(100)  NULL,          -- solo aplica a área Producción
        Amount              DECIMAL(18,2)  NULL,
        Quantity            DECIMAL(18,2)  NULL,
        RegisteredTime      TIME(0)        NULL,          -- antes int de minutos
        ClosedTime          TIME(0)        NULL,          -- antes int de minutos
        EstimatedCloseDate  DATE           NULL,
        CreatedAt           DATETIME2(0)   NOT NULL CONSTRAINT DF_Tickets_CreatedAt DEFAULT (SYSUTCDATETIME()),
        ClosedAt            DATETIME2(0)   NULL,
        SourceDatabase      NVARCHAR(50)   NULL,
        IsActive            BIT            NOT NULL CONSTRAINT DF_Tickets_IsActive DEFAULT (1),
        CONSTRAINT FK_Tickets_TicketTypes    FOREIGN KEY (TicketTypeId) REFERENCES dbo.TicketTypes (Id),
        CONSTRAINT FK_Tickets_TicketStatuses FOREIGN KEY (StatusId)     REFERENCES dbo.TicketStatuses (Id)
    );

    CREATE INDEX IX_Tickets_TicketTypeId        ON dbo.Tickets (TicketTypeId);
    CREATE INDEX IX_Tickets_RequesterUserCode   ON dbo.Tickets (RequesterUserCode);
    CREATE INDEX IX_Tickets_ResponsibleUserCode ON dbo.Tickets (ResponsibleUserCode);
    CREATE INDEX IX_Tickets_DepartmentCode      ON dbo.Tickets (DepartmentCode);
    CREATE INDEX IX_Tickets_List ON dbo.Tickets (IsActive, StatusId, CreatedAt DESC);
END
GO

/* ---------- 6. Comentarios (antes @GP_CHAT_TICKETS) ---------- */
IF OBJECT_ID('dbo.TicketComments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TicketComments
    (
        Id             INT           NOT NULL IDENTITY(1,1) CONSTRAINT PK_TicketComments PRIMARY KEY,
        TicketId       INT           NOT NULL,
        AuthorUserCode NVARCHAR(50)  NOT NULL,
        Body           NVARCHAR(MAX) NOT NULL,
        CreatedAt      DATETIME2(0)  NOT NULL CONSTRAINT DF_TicketComments_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_TicketComments_Tickets FOREIGN KEY (TicketId) REFERENCES dbo.Tickets (Id)
    );
    CREATE INDEX IX_TicketComments_TicketId ON dbo.TicketComments (TicketId);
END
GO

/* =====================================================================
   DATOS SEMILLA
   ===================================================================== */

/* Estatus (Ids estables usados por el enum TicketStatus en C#) */
MERGE dbo.TicketStatuses AS target
USING (VALUES
    (1, N'A',  N'Abierto'),
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

/* Ticket de ejemplo */
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
