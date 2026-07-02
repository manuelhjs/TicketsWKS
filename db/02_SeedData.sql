/* =====================================================================
   Datos semilla del módulo de Tickets. Ejecutar después de 01_Schema.sql.
   ===================================================================== */
USE B1_PROA_MX_V2;
GO

INSERT INTO dbo.Prioridad (Id, Nombre, Descripcion, Orden) VALUES
    (1, N'Alto',  N'Detiene la operación — atención de 1 a 4 horas.',  1),
    (2, N'Medio', N'No detiene la operación — atención de 1 a 2 días.', 2),
    (3, N'Bajo',  N'Atención de 1 a 5 días.',                           3);
GO

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

INSERT INTO dbo.Clasificacion (Nombre) VALUES
    (N'SAP'), (N'CRM'), (N'QUOTE'), (N'FACTURACIÓN'), (N'COMPRAS'), (N'LOGÍSTICA');
GO

INSERT INTO dbo.Categoria (ClasificacionId, Nombre)
SELECT Id, N'General' FROM dbo.Clasificacion;
INSERT INTO dbo.Categoria (ClasificacionId, Nombre)
SELECT Id, N'Acceso / Permisos' FROM dbo.Clasificacion WHERE Nombre IN (N'SAP', N'CRM');
GO

INSERT INTO dbo.Empleados (Codigo, Nombre, Correo, Telefono) VALUES
    (N'blozano', N'Benjamín Lozano', N'blozano@impulsoraint.com', N'5512345678'),
    (N'jperez',  N'Juan Pérez',      N'jperez@impulsoraint.com',  NULL),
    (NULL,       N'María García',    NULL,                        NULL);
GO
