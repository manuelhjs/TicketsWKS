/* =====================================================================
   Módulo de Tickets — Esquema (SQL Server). Recrea los objetos del módulo.
   Las tablas viven dentro de B1_PROA_MX_V2. Ver TicketsDb_Full.sql para el
   script consolidado (esquema + datos). ADVERTENCIA: elimina y recrea tablas.
   ===================================================================== */
USE B1_PROA_MX_V2;
GO

DROP TABLE IF EXISTS dbo.TicketLog;
DROP TABLE IF EXISTS dbo.Adjuntos;
DROP TABLE IF EXISTS dbo.HistorialEstatus;
DROP TABLE IF EXISTS dbo.TicketComments;
DROP TABLE IF EXISTS dbo.Tickets;
DROP TABLE IF EXISTS dbo.Categoria;
DROP TABLE IF EXISTS dbo.Clasificacion;
DROP TABLE IF EXISTS dbo.Prioridad;
DROP TABLE IF EXISTS dbo.Estatus;
DROP TABLE IF EXISTS dbo.Empleados;
DROP TABLE IF EXISTS dbo.TicketTypes;      -- obsoleta
DROP TABLE IF EXISTS dbo.Areas;            -- obsoleta
DROP TABLE IF EXISTS dbo.TicketStatuses;   -- obsoleta
GO

CREATE TABLE dbo.Empleados
(
    Id        INT           NOT NULL IDENTITY(1,1) CONSTRAINT PK_Empleados PRIMARY KEY,
    Codigo    NVARCHAR(50)  NULL,
    Nombre    NVARCHAR(200) NOT NULL,
    Correo    NVARCHAR(256) NULL,
    Telefono  NVARCHAR(20)  NULL,
    Activo    BIT           NOT NULL CONSTRAINT DF_Empleados_Activo DEFAULT (1),
    FechaAlta DATETIME2(0)  NOT NULL CONSTRAINT DF_Empleados_FechaAlta DEFAULT (SYSUTCDATETIME())
);
CREATE INDEX IX_Empleados_Codigo ON dbo.Empleados (Codigo);
CREATE INDEX IX_Empleados_Nombre ON dbo.Empleados (Nombre);
GO

CREATE TABLE dbo.Clasificacion
(
    Id        INT           NOT NULL IDENTITY(1,1) CONSTRAINT PK_Clasificacion PRIMARY KEY,
    Nombre    NVARCHAR(100) NOT NULL,
    Activo    BIT           NOT NULL CONSTRAINT DF_Clasificacion_Activo DEFAULT (1),
    FechaAlta DATETIME2(0)  NOT NULL CONSTRAINT DF_Clasificacion_FechaAlta DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT UQ_Clasificacion_Nombre UNIQUE (Nombre)
);
GO

CREATE TABLE dbo.Categoria
(
    Id              INT           NOT NULL IDENTITY(1,1) CONSTRAINT PK_Categoria PRIMARY KEY,
    ClasificacionId INT           NOT NULL,
    Nombre          NVARCHAR(150) NOT NULL,
    Activo          BIT           NOT NULL CONSTRAINT DF_Categoria_Activo DEFAULT (1),
    FechaAlta       DATETIME2(0)  NOT NULL CONSTRAINT DF_Categoria_FechaAlta DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_Categoria_Clasificacion FOREIGN KEY (ClasificacionId) REFERENCES dbo.Clasificacion (Id),
    CONSTRAINT UQ_Categoria_Clasif_Nombre UNIQUE (ClasificacionId, Nombre)
);
CREATE INDEX IX_Categoria_ClasificacionId ON dbo.Categoria (ClasificacionId);
GO

CREATE TABLE dbo.Prioridad
(
    Id          TINYINT       NOT NULL CONSTRAINT PK_Prioridad PRIMARY KEY,
    Nombre      NVARCHAR(30)  NOT NULL,
    Descripcion NVARCHAR(300) NOT NULL,
    Orden       TINYINT       NOT NULL
);
GO

CREATE TABLE dbo.Estatus
(
    Id      TINYINT      NOT NULL CONSTRAINT PK_Estatus PRIMARY KEY,
    Nombre  NVARCHAR(50) NOT NULL,
    Orden   TINYINT      NOT NULL,
    EsFinal BIT          NOT NULL CONSTRAINT DF_Estatus_EsFinal DEFAULT (0),
    CONSTRAINT UQ_Estatus_Nombre UNIQUE (Nombre)
);
GO

CREATE TABLE dbo.Tickets
(
    Id                    INT            NOT NULL IDENTITY(1,1) CONSTRAINT PK_Tickets PRIMARY KEY,
    SolicitanteId         INT            NOT NULL,
    Correo                NVARCHAR(256)  NULL,
    Celular               NVARCHAR(10)   NULL,
    TipoSolicitud         TINYINT        NOT NULL,
    ClasificacionId       INT            NOT NULL,
    CategoriaId           INT            NOT NULL,
    PrioridadId           TINYINT        NOT NULL,
    EstatusId             TINYINT        NOT NULL CONSTRAINT DF_Tickets_EstatusId DEFAULT (1),
    ResponsableEmpleadoId INT            NULL,
    Descripcion           NVARCHAR(2000) NOT NULL,
    CreatedAt             DATETIME2(0)   NOT NULL CONSTRAINT DF_Tickets_CreatedAt DEFAULT (SYSUTCDATETIME()),
    ClosedAt              DATETIME2(0)   NULL,
    IsActive              BIT            NOT NULL CONSTRAINT DF_Tickets_IsActive DEFAULT (1),
    CONSTRAINT FK_Tickets_Solicitante   FOREIGN KEY (SolicitanteId)         REFERENCES dbo.Empleados (Id),
    CONSTRAINT FK_Tickets_Responsable   FOREIGN KEY (ResponsableEmpleadoId) REFERENCES dbo.Empleados (Id),
    CONSTRAINT FK_Tickets_Clasificacion FOREIGN KEY (ClasificacionId)       REFERENCES dbo.Clasificacion (Id),
    CONSTRAINT FK_Tickets_Categoria     FOREIGN KEY (CategoriaId)           REFERENCES dbo.Categoria (Id),
    CONSTRAINT FK_Tickets_Prioridad     FOREIGN KEY (PrioridadId)           REFERENCES dbo.Prioridad (Id),
    CONSTRAINT FK_Tickets_Estatus       FOREIGN KEY (EstatusId)             REFERENCES dbo.Estatus (Id),
    CONSTRAINT CK_Tickets_TipoSolicitud CHECK (TipoSolicitud IN (1, 2))
);
CREATE INDEX IX_Tickets_SolicitanteId   ON dbo.Tickets (SolicitanteId);
CREATE INDEX IX_Tickets_EstatusId       ON dbo.Tickets (EstatusId);
CREATE INDEX IX_Tickets_ClasificacionId ON dbo.Tickets (ClasificacionId);
CREATE INDEX IX_Tickets_CategoriaId     ON dbo.Tickets (CategoriaId);
CREATE INDEX IX_Tickets_List            ON dbo.Tickets (IsActive, EstatusId, CreatedAt DESC);
GO

CREATE TABLE dbo.TicketComments
(
    Id          INT           NOT NULL IDENTITY(1,1) CONSTRAINT PK_TicketComments PRIMARY KEY,
    TicketId    INT           NOT NULL,
    AutorCodigo NVARCHAR(50)  NOT NULL,
    Comentario  NVARCHAR(MAX) NOT NULL,
    CreatedAt   DATETIME2(0)  NOT NULL CONSTRAINT DF_TicketComments_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_TicketComments_Tickets FOREIGN KEY (TicketId) REFERENCES dbo.Tickets (Id)
);
CREATE INDEX IX_TicketComments_TicketId ON dbo.TicketComments (TicketId);
GO

CREATE TABLE dbo.HistorialEstatus
(
    Id                INT            NOT NULL IDENTITY(1,1) CONSTRAINT PK_HistorialEstatus PRIMARY KEY,
    TicketId          INT            NOT NULL,
    EstatusAnteriorId TINYINT        NULL,
    EstatusNuevoId    TINYINT        NOT NULL,
    Comentario        NVARCHAR(1000) NOT NULL,
    UsuarioCodigo     NVARCHAR(50)   NOT NULL,
    Fecha             DATETIME2(0)   NOT NULL CONSTRAINT DF_HistorialEstatus_Fecha DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_HistorialEstatus_Tickets FOREIGN KEY (TicketId)          REFERENCES dbo.Tickets (Id),
    CONSTRAINT FK_HistorialEstatus_EstAnt  FOREIGN KEY (EstatusAnteriorId) REFERENCES dbo.Estatus (Id),
    CONSTRAINT FK_HistorialEstatus_EstNue  FOREIGN KEY (EstatusNuevoId)    REFERENCES dbo.Estatus (Id)
);
CREATE INDEX IX_HistorialEstatus_TicketId ON dbo.HistorialEstatus (TicketId);
GO

CREATE TABLE dbo.Adjuntos
(
    Id               INT           NOT NULL IDENTITY(1,1) CONSTRAINT PK_Adjuntos PRIMARY KEY,
    TicketId         INT           NOT NULL,
    NombreOriginal   NVARCHAR(260) NOT NULL,
    NombreAlmacenado NVARCHAR(260) NOT NULL,
    TipoContenido    NVARCHAR(150) NULL,
    TamanoBytes      BIGINT        NOT NULL,
    UsuarioCodigo    NVARCHAR(50)  NULL,
    Fecha            DATETIME2(0)  NOT NULL CONSTRAINT DF_Adjuntos_Fecha DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_Adjuntos_Tickets FOREIGN KEY (TicketId) REFERENCES dbo.Tickets (Id)
);
CREATE INDEX IX_Adjuntos_TicketId ON dbo.Adjuntos (TicketId);
GO

CREATE TABLE dbo.TicketLog
(
    Id            BIGINT        NOT NULL IDENTITY(1,1) CONSTRAINT PK_TicketLog PRIMARY KEY,
    TicketId      INT           NOT NULL,
    Accion        NVARCHAR(50)  NOT NULL,
    Descripcion   NVARCHAR(500) NULL,
    ValorAnterior NVARCHAR(MAX) NULL,
    ValorNuevo    NVARCHAR(MAX) NULL,
    UsuarioCodigo NVARCHAR(50)  NULL,
    FechaHora     DATETIME2(0)  NOT NULL CONSTRAINT DF_TicketLog_FechaHora DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_TicketLog_Tickets FOREIGN KEY (TicketId) REFERENCES dbo.Tickets (Id)
);
CREATE INDEX IX_TicketLog_TicketId ON dbo.TicketLog (TicketId);
GO
