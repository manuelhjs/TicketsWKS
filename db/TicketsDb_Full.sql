/* =====================================================================
   Módulo de Tickets — Script COMPLETO (SQL Server)
   Esquema + datos semilla. Recrea los objetos del módulo (drop + create).

   Las tablas viven DENTRO de la base de SAP B1_PROA_MX_V2 (destino de la
   cadena de conexión TicketsDb). Para una base independiente, cambia el
   bloque USE por: IF DB_ID('TicketsDb') IS NULL CREATE DATABASE TicketsDb; USE TicketsDb;

   Cómo ejecutar: SSMS / Azure Data Studio (F5), o
       sqlcmd -S TU_SERVIDOR -f 65001 -i TicketsDb_Full.sql

   ADVERTENCIA: este script ELIMINA y recrea las tablas del módulo
   (incluye datos). Úsalo en ambientes de desarrollo/pruebas.
   ===================================================================== */

USE B1_PROA_MX_V2;
GO

/* ---------- 1. Limpieza: eliminar objetos del módulo (hijos -> padres) ----------
   Incluye tablas obsoletas de versiones previas (Areas, TicketTypes, TicketStatuses). */
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
-- Obsoletas (diseño anterior)
DROP TABLE IF EXISTS dbo.TicketTypes;
DROP TABLE IF EXISTS dbo.Areas;
DROP TABLE IF EXISTS dbo.TicketStatuses;
GO

/* ---------- 2. Empleados (NUEVA) — fuente de Solicitante/Responsable ---------- */
CREATE TABLE dbo.Empleados
(
    Id           INT           NOT NULL IDENTITY(1,1) CONSTRAINT PK_Empleados PRIMARY KEY,
    Codigo       NVARCHAR(50)  NULL,        -- enlace opcional a usuario SAP (para preselección)
    Nombre       NVARCHAR(200) NOT NULL,
    Correo       NVARCHAR(256) NULL,        -- opcional
    Telefono     NVARCHAR(20)  NULL,        -- opcional
    Puesto       NVARCHAR(150) NULL,
    Area         NVARCHAR(100) NULL,
    FechaIngreso DATE          NULL,
    Activo       BIT           NOT NULL CONSTRAINT DF_Empleados_Activo DEFAULT (1),
    FechaAlta    DATETIME2(0)  NOT NULL CONSTRAINT DF_Empleados_FechaAlta DEFAULT (SYSUTCDATETIME())
);
CREATE INDEX IX_Empleados_Codigo ON dbo.Empleados (Codigo);
CREATE INDEX IX_Empleados_Nombre ON dbo.Empleados (Nombre);
GO

/* ---------- 3. Clasificacion (catálogo editable) ---------- */
CREATE TABLE dbo.Clasificacion
(
    Id        INT           NOT NULL IDENTITY(1,1) CONSTRAINT PK_Clasificacion PRIMARY KEY,
    Nombre    NVARCHAR(100) NOT NULL,
    Activo    BIT           NOT NULL CONSTRAINT DF_Clasificacion_Activo DEFAULT (1),
    FechaAlta DATETIME2(0)  NOT NULL CONSTRAINT DF_Clasificacion_FechaAlta DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT UQ_Clasificacion_Nombre UNIQUE (Nombre)
);
GO

/* ---------- 4. Categoria (catálogo editable, FK a Clasificacion) ---------- */
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

/* ---------- 5. Prioridad (catálogo fijo, con texto SLA) ---------- */
CREATE TABLE dbo.Prioridad
(
    Id          TINYINT       NOT NULL CONSTRAINT PK_Prioridad PRIMARY KEY,
    Nombre      NVARCHAR(30)  NOT NULL,
    Descripcion NVARCHAR(300) NOT NULL,   -- descripción + SLA de referencia
    Orden       TINYINT       NOT NULL,
    Activo      BIT           NOT NULL CONSTRAINT DF_Prioridad_Activo DEFAULT (1)
);
GO

/* ---------- 6. Estatus (catálogo del ciclo de vida del ticket) ---------- */
CREATE TABLE dbo.Estatus
(
    Id      TINYINT      NOT NULL CONSTRAINT PK_Estatus PRIMARY KEY,
    Nombre  NVARCHAR(50) NOT NULL,
    Orden   TINYINT      NOT NULL,
    EsFinal BIT          NOT NULL CONSTRAINT DF_Estatus_EsFinal DEFAULT (0),
    Activo  BIT          NOT NULL CONSTRAINT DF_Estatus_Activo DEFAULT (1),
    CONSTRAINT UQ_Estatus_Nombre UNIQUE (Nombre)
);
GO

/* ---------- 7. Tickets ---------- */
CREATE TABLE dbo.Tickets
(
    Id                    INT           NOT NULL IDENTITY(1,1) CONSTRAINT PK_Tickets PRIMARY KEY,
    SolicitanteId         INT           NOT NULL,
    Correo                NVARCHAR(256) NULL,
    Celular               NVARCHAR(10)  NULL,
    TipoSolicitud         TINYINT       NOT NULL,   -- 1 = Incidencia, 2 = Requerimiento
    ClasificacionId       INT           NOT NULL,
    CategoriaId           INT           NOT NULL,
    PrioridadId           TINYINT       NOT NULL,
    EstatusId             TINYINT       NOT NULL CONSTRAINT DF_Tickets_EstatusId DEFAULT (1),  -- Por asignar
    ResponsableEmpleadoId INT           NULL,
    Descripcion           NVARCHAR(2000) NOT NULL,
    CreatedAt             DATETIME2(0)  NOT NULL CONSTRAINT DF_Tickets_CreatedAt DEFAULT (SYSUTCDATETIME()),
    ClosedAt              DATETIME2(0)  NULL,
    IsActive              BIT           NOT NULL CONSTRAINT DF_Tickets_IsActive DEFAULT (1),
    CONSTRAINT FK_Tickets_Solicitante   FOREIGN KEY (SolicitanteId)         REFERENCES dbo.Empleados (Id),
    CONSTRAINT FK_Tickets_Responsable   FOREIGN KEY (ResponsableEmpleadoId) REFERENCES dbo.Empleados (Id),
    CONSTRAINT FK_Tickets_Clasificacion FOREIGN KEY (ClasificacionId)       REFERENCES dbo.Clasificacion (Id),
    CONSTRAINT FK_Tickets_Categoria     FOREIGN KEY (CategoriaId)           REFERENCES dbo.Categoria (Id),
    CONSTRAINT FK_Tickets_Prioridad     FOREIGN KEY (PrioridadId)           REFERENCES dbo.Prioridad (Id),
    CONSTRAINT FK_Tickets_Estatus       FOREIGN KEY (EstatusId)             REFERENCES dbo.Estatus (Id),
    CONSTRAINT CK_Tickets_TipoSolicitud CHECK (TipoSolicitud IN (1, 2))
);
CREATE INDEX IX_Tickets_SolicitanteId ON dbo.Tickets (SolicitanteId);
CREATE INDEX IX_Tickets_EstatusId     ON dbo.Tickets (EstatusId);
CREATE INDEX IX_Tickets_ClasificacionId ON dbo.Tickets (ClasificacionId);
CREATE INDEX IX_Tickets_CategoriaId   ON dbo.Tickets (CategoriaId);
CREATE INDEX IX_Tickets_List          ON dbo.Tickets (IsActive, EstatusId, CreatedAt DESC);
GO

/* ---------- 8. TicketComments (comentarios generales) ---------- */
CREATE TABLE dbo.TicketComments
(
    Id             INT           NOT NULL IDENTITY(1,1) CONSTRAINT PK_TicketComments PRIMARY KEY,
    TicketId       INT           NOT NULL,
    AutorCodigo    NVARCHAR(50)  NOT NULL,   -- usuario que comenta (código actual)
    Comentario     NVARCHAR(MAX) NOT NULL,
    CreatedAt      DATETIME2(0)  NOT NULL CONSTRAINT DF_TicketComments_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_TicketComments_Tickets FOREIGN KEY (TicketId) REFERENCES dbo.Tickets (Id)
);
CREATE INDEX IX_TicketComments_TicketId ON dbo.TicketComments (TicketId);
GO

/* ---------- 9. HistorialEstatus (bitácora específica de estatus) ---------- */
CREATE TABLE dbo.HistorialEstatus
(
    Id                INT           NOT NULL IDENTITY(1,1) CONSTRAINT PK_HistorialEstatus PRIMARY KEY,
    TicketId          INT           NOT NULL,
    EstatusAnteriorId TINYINT       NULL,
    EstatusNuevoId    TINYINT       NOT NULL,
    Comentario        NVARCHAR(1000) NOT NULL,   -- obligatorio al cambiar de estatus
    UsuarioCodigo     NVARCHAR(50)  NOT NULL,
    Fecha             DATETIME2(0)  NOT NULL CONSTRAINT DF_HistorialEstatus_Fecha DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_HistorialEstatus_Tickets  FOREIGN KEY (TicketId)          REFERENCES dbo.Tickets (Id),
    CONSTRAINT FK_HistorialEstatus_EstAnt   FOREIGN KEY (EstatusAnteriorId) REFERENCES dbo.Estatus (Id),
    CONSTRAINT FK_HistorialEstatus_EstNue   FOREIGN KEY (EstatusNuevoId)    REFERENCES dbo.Estatus (Id)
);
CREATE INDEX IX_HistorialEstatus_TicketId ON dbo.HistorialEstatus (TicketId);
GO

/* ---------- 10. Adjuntos (evidencias, múltiples por ticket) ---------- */
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

/* ---------- 11. TicketLog (auditoría general, solo-inserción) ---------- */
CREATE TABLE dbo.TicketLog
(
    Id            BIGINT        NOT NULL IDENTITY(1,1) CONSTRAINT PK_TicketLog PRIMARY KEY,
    TicketId      INT           NOT NULL,
    Accion        NVARCHAR(50)  NOT NULL,   -- Creacion, EdicionCampo, CambioEstatus, AsignaResponsable, AdjuntoAgregado, ...
    Descripcion   NVARCHAR(500) NULL,
    ValorAnterior NVARCHAR(MAX) NULL,
    ValorNuevo    NVARCHAR(MAX) NULL,
    UsuarioCodigo NVARCHAR(50)  NULL,
    FechaHora     DATETIME2(0)  NOT NULL CONSTRAINT DF_TicketLog_FechaHora DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_TicketLog_Tickets FOREIGN KEY (TicketId) REFERENCES dbo.Tickets (Id)
);
CREATE INDEX IX_TicketLog_TicketId ON dbo.TicketLog (TicketId);
GO

/* =====================================================================
   DATOS SEMILLA
   ===================================================================== */

/* Prioridad (con SLA de referencia) */
INSERT INTO dbo.Prioridad (Id, Nombre, Descripcion, Orden) VALUES
    (1, N'Alto',  N'Detiene la operación — atención de 1 a 4 horas.',        1),
    (2, N'Medio', N'No detiene la operación — atención de 1 a 2 días.',       2),
    (3, N'Bajo',  N'Atención de 1 a 5 días.',                                 3);
GO

/* Estatus (ciclo de vida) */
INSERT INTO dbo.Estatus (Id, Nombre, Orden, EsFinal) VALUES
    (1,  N'Por asignar',     1,  0),
    (2,  N'Asignado',        2,  0),
    (3,  N'Análisis',        3,  0),
    (4,  N'Desarrollo',      4,  0),
    (5,  N'Pruebas TI',      5,  0),
    (6,  N'Pruebas Usuario', 6,  0),
    (7,  N'Cotización',      7,  0),
    (8,  N'Autorización',    8,  0),
    (9,  N'Pausa',           9,  0),
    (10, N'En proceso',     10,  0),
    (11, N'Cancelado',      11,  1),
    (12, N'Finalizado',     12,  1);
GO

/* Clasificación (crece por "Otro") */
INSERT INTO dbo.Clasificacion (Nombre) VALUES
    (N'SAP'), (N'CRM'), (N'QUOTE'), (N'FACTURACIÓN'), (N'COMPRAS'), (N'LOGÍSTICA');
GO

/* Categorías de ejemplo por clasificación */
INSERT INTO dbo.Categoria (ClasificacionId, Nombre)
SELECT Id, N'General' FROM dbo.Clasificacion;
INSERT INTO dbo.Categoria (ClasificacionId, Nombre)
SELECT Id, N'Acceso / Permisos' FROM dbo.Clasificacion WHERE Nombre IN (N'SAP', N'CRM');
GO

/* Empleados de ejemplo */
INSERT INTO dbo.Empleados (Codigo, Nombre, Correo, Telefono) VALUES
    (N'blozano', N'Benjamín Lozano', N'blozano@impulsoraint.com', N'5512345678'),
    (N'jperez',  N'Juan Pérez',      N'jperez@impulsoraint.com',  NULL),
    (NULL,       N'María García',    NULL,                        NULL);
GO
