/* =====================================================================
   Módulo de Tickets — Esquema nuevo (SQL Server)
   ---------------------------------------------------------------------
   Referencia FUNCIONAL: @GP_TICKETS, @GP_ASIGNACIONTP, @GP_CHAT_TICKETS
   de PROAMASTER (SAP Business One). NO se replica su modelado.

   Decisiones de alcance:
   - Usuarios y Departamentos siguen siendo maestros EXTERNOS de SAP.
     Se referencian por su código (nvarchar), sin recrearlos aquí.
   - Se crean tablas propias solo para lo que pertenece al módulo:
     Areas, TicketTypes, Tickets, TicketComments (+ catálogo TicketStatuses).

   Correcciones aplicadas frente al esquema original (ver README):
   - PK "Id" INT IDENTITY (no "Code" varchar generado por SELECT MAX+1).
   - Sin prefijos "@", "U_" ni "VL_"; sin caracteres especiales.
   - Tipos correctos: TIME para horas (no int de minutos), BIT para
     banderas (no 'A'/'IN'), DATE/DATETIME2 para fechas.
   - Estatus normalizado en catálogo con FK (no magic strings 'A'/'C'/'EP').
   - Claves foráneas reales entre Tickets, TicketTypes y Areas.
   - Índices en columnas de filtro y FKs.
   ===================================================================== */

IF DB_ID('TicketsDb') IS NULL
    CREATE DATABASE TicketsDb;
GO

USE TicketsDb;
GO

/* ---------------------------------------------------------------------
   Catálogo de estatus (Abierto / En Proceso / Cerrado)
   Reemplaza los magic strings 'A' / 'EP' / 'C' del original.
   --------------------------------------------------------------------- */
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

/* ---------------------------------------------------------------------
   Áreas (antes VW_GP_AREAS). Ej.: CAL = Calidad, PD = Producción.
   --------------------------------------------------------------------- */
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

/* ---------------------------------------------------------------------
   Tipos de solicitud (antes @GP_ASIGNACIONTP).
   Cada tipo pertenece a un área y define un responsable por defecto
   (código de usuario externo de SAP).
   --------------------------------------------------------------------- */
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

/* ---------------------------------------------------------------------
   Tickets (antes @GP_TICKETS).
   RequesterUserCode / ResponsibleUserCode / DepartmentCode son
   referencias externas a maestros de SAP (no hay FK local a propósito).
   --------------------------------------------------------------------- */
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
        Category            NVARCHAR(20)   NULL,          -- antes U_Tipo (valor de dominio, p.ej. 'S')
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
        CONSTRAINT FK_Tickets_TicketTypes  FOREIGN KEY (TicketTypeId) REFERENCES dbo.TicketTypes (Id),
        CONSTRAINT FK_Tickets_TicketStatuses FOREIGN KEY (StatusId)   REFERENCES dbo.TicketStatuses (Id)
    );

    CREATE INDEX IX_Tickets_TicketTypeId        ON dbo.Tickets (TicketTypeId);
    CREATE INDEX IX_Tickets_RequesterUserCode   ON dbo.Tickets (RequesterUserCode);
    CREATE INDEX IX_Tickets_ResponsibleUserCode ON dbo.Tickets (ResponsibleUserCode);
    CREATE INDEX IX_Tickets_DepartmentCode      ON dbo.Tickets (DepartmentCode);
    -- Índice de apoyo para el listado principal (activos, por estatus y fecha)
    CREATE INDEX IX_Tickets_List ON dbo.Tickets (IsActive, StatusId, CreatedAt DESC);
END
GO

/* ---------------------------------------------------------------------
   Comentarios de ticket (antes @GP_CHAT_TICKETS).
   --------------------------------------------------------------------- */
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
