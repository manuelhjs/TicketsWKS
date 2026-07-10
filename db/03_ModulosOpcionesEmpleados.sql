/* =====================================================================
   Incremental para los módulos "Opciones de Tickets" y "Empleados".
   Aplica sobre una BD existente (no recrea nada). Idempotente.
   ===================================================================== */
USE B1_PROA_MX_V2;
GO

/* Prioridad / Estatus: borrado lógico (columna Activo) */
IF COL_LENGTH('dbo.Prioridad', 'Activo') IS NULL
    ALTER TABLE dbo.Prioridad ADD Activo BIT NOT NULL CONSTRAINT DF_Prioridad_Activo DEFAULT (1);
GO
IF COL_LENGTH('dbo.Estatus', 'Activo') IS NULL
    ALTER TABLE dbo.Estatus ADD Activo BIT NOT NULL CONSTRAINT DF_Estatus_Activo DEFAULT (1);
GO

/* Empleados: campos del módulo de Empleados */
IF COL_LENGTH('dbo.Empleados', 'Puesto') IS NULL
    ALTER TABLE dbo.Empleados ADD Puesto NVARCHAR(150) NULL;
GO
IF COL_LENGTH('dbo.Empleados', 'Area') IS NULL
    ALTER TABLE dbo.Empleados ADD Area NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.Empleados', 'FechaIngreso') IS NULL
    ALTER TABLE dbo.Empleados ADD FechaIngreso DATE NULL;
GO
