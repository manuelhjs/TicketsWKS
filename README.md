# Módulo de Tickets — .NET 10 (reescritura limpia)

Reimplementación del módulo `Tickets/Tickets` de **PROAMASTER** (legacy en .NET Framework 4.8 + ASP.NET MVC 5 + SAP Business One). El código legacy se usó **solo como referencia funcional**; la arquitectura y el modelado son nuevos.

## Stack

- **.NET 10 MVC**
- **Dapper** (acceso a datos parametrizado)
- **Serilog** (structured logging: consola + archivo rotativo diario)
- **Bootstrap 5** + **JavaScript** (vanilla + fetch)
- **xUnit + Moq** (pruebas)

## Arquitectura por capas (proyectos separados)

```
src/
  Tickets.Domain          Entidades y enums puros (sin dependencias)
  Tickets.Application      Interfaces (repos + services), DTOs/ViewModels, lógica de negocio, excepciones
  Tickets.Infrastructure   Repositorios Dapper, fábrica de conexiones, type handlers, DI
  Tickets.Web             MVC: controladores delgados, vistas, JS/CSS, middleware de errores, Serilog
tests/
  Tickets.Tests           Pruebas unitarias de los servicios (dependencias mockeadas)
db/
  01_Schema.sql           Tablas nuevas
  02_SeedData.sql         Catálogos + datos de ejemplo
```

Regla de dependencias: `Web → Application → Domain` y `Web → Infrastructure → Application → Domain`. El dominio no depende de nada; la aplicación no conoce a la infraestructura (solo sus interfaces).

### Principios aplicados

- **Inyección de dependencias** nativa de .NET en todas las capas.
- **Repository pattern** con Dapper; **Services** con la lógica de negocio; **controladores delgados** que solo orquestan.
- **Interfaces** para cada service y repository → testabilidad (ver `tests/`).
- **DTOs/ViewModels** separados de las entidades de BD.
- **Manejo de errores centralizado** en `ExceptionHandlingMiddleware` (traduce excepciones a HTTP + JSON; sin `try/catch` por acción).
- **Serilog** con niveles configurables y logging estructurado por petición.

## Cómo ejecutar

### 1. Base de datos

Con **LocalDB** (o cualquier SQL Server). Ejecuta los scripts **preservando UTF-8** (por los acentos):

```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i db/01_Schema.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i db/02_SeedData.sql
```

> O ábrelos en SSMS / Azure Data Studio (que ya manejan UTF-8) y ejecútalos.

### 2. Configuración

Cadenas de conexión (fijas a la base de SAP **B1_PROA_MX_V2**):

- `appsettings.json` → servidor de **test/QA** `192.168.7.62` (usuario `sa`). Es la base por defecto y la que hereda *Development*.
- `appsettings.Production.json` → servidor de **producción** `db1-19.doorgroup.local` (usuario `sap_sa_2`). Se aplica al correr con `ASPNETCORE_ENVIRONMENT=Production`.
- Las tablas nuevas viven **dentro de B1_PROA_MX_V2** (junto a las de SAP); ejecuta ahí el script `db/TicketsDb_Full.sql`.

> ⚠️ **Seguridad:** las credenciales están en texto plano (por paridad con PROAMASTER). Para un despliegue real, muévelas a **User Secrets** / variables de entorno / Azure Key Vault, y **no** subas los `appsettings*.json` con contraseñas al control de versiones.

- `CurrentUser`: usuario stub (no hay auth en esta versión). Controla visibilidad/permisos.
- `ConnectionStrings:SapDirectory`: apunta a la misma B1_PROA_MX_V2, de modo que el nombre, departamento y puesto (solicitante/responsable/usuario actual) se resuelven desde la vista `dbo.VL_Usuarios` (columnas `Code`, `Nombre`, `Correo`, `DepCode`, `DepName`, `PuestoTexto`; `Sap:UsersView` = `dbo.VL_Usuarios`). Si se deja vacío, los nombres se muestran con el código.
- `Attachments:Path`: carpeta de adjuntos (por defecto `<contentRoot>/attachments`).

### 3. Ejecutar

```bash
cd src/Tickets.Web
dotnet run
```

### 4. Pruebas

```bash
dotnet test
```

## Decisiones de alcance

| Tema | Decisión |
|---|---|
| Usuarios / Departamentos | **Externos de SAP** (referenciados por código, sin recrearlos). |
| Autenticación / roles | **Sin auth y sin roles**: `ICurrentUserService` con stub configurable; acceso completo para todos. |
| Notificaciones por correo | **Omitidas** en esta versión (el legacy enviaba correos desde la entidad). |
| Organización | Multi-proyecto por capas. |

## Modelo de datos (formulario de tickets)

Tablas del módulo (en `B1_PROA_MX_V2`): `Empleados`, `Clasificacion`, `Categoria` (FK→Clasificacion), `Prioridad`, `Estatus`, `Tickets`, `TicketComments`, `HistorialEstatus`, `Adjuntos`, `TicketLog`.

- **Solicitante** → `Empleados` (alta rápida desde el formulario; Correo/Teléfono opcionales). El usuario actual se auto-provisiona como Empleado y queda preseleccionado.
- **Clasificación / Categoría** → catálogos **editables** con opción “Otro” que da de alta el valor (Categoría depende de Clasificación en cascada).
- **Prioridad / Estatus** → catálogos. Estatus: 12 valores (Por asignar … Finalizado).
- **Auditoría (separada):** `HistorialEstatus` registra cada cambio de estatus (**comentario obligatorio**); `TicketLog` registra el resto de acciones (creación, edición campo a campo con valor anterior/nuevo, asignación de responsable, adjuntos, comentarios). El log es de **solo inserción**.
- **Adjuntos:** múltiples por ticket. Límite **10 MB/archivo, 5 archivos**; tipos permitidos PDF/JPG/PNG/PPT(X)/DOC(X)/XLS(X)/TXT/CSV. Validado en **backend** (`AttachmentRules`) además del cliente; si no se puede adjuntar, leyenda alterna (Google Drive / plataformas.soporte@impulsoraint.com).

> El script `db/TicketsDb_Full.sql` **recrea** (drop + create) los objetos del módulo, incluidas las tablas obsoletas del diseño anterior (`Areas`, `TicketTypes`, `TicketStatuses`).

## Vista principal (UI)

- **Sin roles:** todos los usuarios tienen acceso completo.
- **Cabecera de usuario:** nombre completo, **nombre** del departamento y puesto, resueltos de `dbo.VL_Usuarios` (que ya expone `DepName` vía `OOCR` y `PuestoTexto`); `CurrentUser` es respaldo.
- **Creación:** formulario **embebido, visible por defecto**, antes de los filtros. Campos: solicitante, correo (autocompletado/editable), celular (10 dígitos), tipo de solicitud (Incidencia/Requerimiento con tooltip), prioridad (Alto/Medio/Bajo con SLA en tooltip), clasificación, categoría (cascada), descripción (máx. 2000, contador), evidencias. Leyendas fijas de horario y evaluación de prioridad. Validaciones en cliente **y backend**.
- **Detalle/edición:** clic en fila o botón **Ver** abre un modal con pestañas: **Detalle** (edita campos + cambia estatus con comentario obligatorio + asigna responsable + adjuntos + inactivar), **Comentarios**, **Historial de estatus**, **Bitácora** (TicketLog).
- **Tabla:** **DataTables** (Bootstrap 5) — paginación/búsqueda/orden client-side sobre el resultado ya filtrado en SQL (tope 5000 filas; migrar a server-side si crece).

## Módulos de administración (menú principal)

Además de **Tickets**, la barra superior incluye dos módulos independientes con el mismo diseño y patrones:

- **Opciones de Tickets** (`/Options`): administra los catálogos que alimentan el alta de tickets, en 4 pestañas — **Clasificaciones, Categorías, Prioridades, Estatus**. Permite listar, agregar, editar y **activar/desactivar** (borrado **lógico**: desactivar oculta la opción del alta sin afectar tickets existentes). Categoría depende de Clasificación. Prioridad y Estatus admiten `Orden`; Estatus además marca "cierra el ticket".
- **Empleados** (`/Empleados`): **reutiliza la misma tabla `Empleados`** que el "Solicitante" de tickets (no duplica la entidad). Se le agregaron `Puesto`, `Area`, `FechaIngreso`. Ofrece tabla con buscador/paginación (DataTables), alta/edición, activar/desactivar, **exportar CSV** y **importar CSV** (upsert por `Codigo` o `Nombre+Correo`; filas inválidas se omiten y se reporta el detalle). CSV UTF-8 con encabezado: `Codigo, Nombre, Correo, Telefono, Puesto, Area, FechaIngreso, Activo`.

> **Migración de BD:** ejecuta `db/03_ModulosOpcionesEmpleados.sql` sobre una BD existente (agrega `Activo` a Prioridad/Estatus y `Puesto/Area/FechaIngreso` a Empleados). Es idempotente. Si recreas todo con `TicketsDb_Full.sql`, ya incluye estas columnas.

## Correspondencia legacy → nuevo

| Legacy (PROAMASTER) | Nuevo |
|---|---|
| `@GP_TICKETS` | `dbo.Tickets` (rediseñada) |
| `@GP_ASIGNACIONTP` / áreas | `dbo.Clasificacion` + `dbo.Categoria` |
| `@GP_CHAT_TICKETS` | `dbo.TicketComments` |
| — (nuevo) | `Empleados`, `Prioridad`, `Estatus`, `HistorialEstatus`, `Adjuntos`, `TicketLog` |
| `VL_Usuarios` / `OOCR` (SAP) | Externas (solo lectura vía `IUserDirectoryRepository`) |
| `Ticket.cs` (890 líneas: entidad+datos+negocio+correo) | `Domain.Ticket` + `TicketService` + `TicketRepository` |
| `BD.cs` (ADO.NET) | Dapper + `SqlConnectionFactory` |
| `TicketsController` con lógica y `try/catch` | Controlador delgado + middleware de errores |

## Malas prácticas del legacy y su corrección

| Mala práctica detectada | Corrección |
|---|---|
| **Inyección SQL** por interpolación de strings con datos de usuario | Dapper 100% parametrizado (`DynamicParameters`) |
| PK `Code` (varchar) generada con `SELECT MAX+1` en la app (condición de carrera, tope "≤327→328") | `Id INT IDENTITY` |
| Horas como `int` (minutos), fechas/booleanos como `varchar` (`'A'/'IN'`) | `TIME`, `DATETIME2`, `DATE`, `BIT` |
| Estatus como magic strings `'A'/'C'/'EP'` | Catálogo `Estatus` con FK |
| Prefijos `@`, `U_`, `VL_`; mezcla español/inglés; `U_DBOrigen` vs `U_BDOrigen` (bug) | Naming limpio y consistente, `Id` como PK |
| Modelo gordo (Active Record): datos+negocio+UI+correo en una clase | Capas separadas (Domain/Application/Infrastructure/Web) |
| Métodos estáticos, `new SqlConnection`/`new Mail` inline → no testeable | DI + interfaces (con pruebas unitarias) |
| `catch { return new List(); }` que ocultaban errores | Middleware central + logging estructurado (Serilog) |
| Autorización hardcodeada en el controlador (`user.Code != "blozano"`) | Reglas en el service vía `ICurrentUserService` |
| Subconsultas correlacionadas por fila y query duplicada en 2 clases | JOINs + catálogos normalizados; resolución de nombres en lote (sin N+1) |
| Credenciales concatenadas en texto plano en la cadena de conexión | `appsettings` / Secrets |
```
