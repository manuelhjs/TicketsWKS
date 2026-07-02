using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Tickets.Application.Abstractions;
using Tickets.Application.Dtos;
using Tickets.Web.Models;
using Tickets.Web.Services;

namespace Tickets.Web.Controllers;

/// <summary>
/// Controlador DELGADO: solo orquesta. La lógica vive en los servicios; los errores
/// se gestionan en el middleware central (sin try/catch por acción, salvo la carga
/// multi-archivo que agrega resultados por archivo).
/// </summary>
public sealed class TicketsController(
    ITicketService ticketService,
    ITicketCommentService commentService,
    ICatalogService catalogService,
    IEmpleadoService empleadoService,
    IAdjuntoService adjuntoService,
    ICurrentUserService currentUser,
    IUserDirectoryRepository userDirectory,
    IFileStorage fileStorage) : Controller
{
    // ---------- Página ----------
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var fullName = currentUser.FullName;
        var departmentName = currentUser.DepartmentName ?? currentUser.DepartmentCode;
        var position = currentUser.Position;

        var me = (await userDirectory.GetByCodesAsync([currentUser.UserCode], ct)).FirstOrDefault();
        if (me is not null)
        {
            if (!string.IsNullOrWhiteSpace(me.Name)) fullName = me.Name;
            if (!string.IsNullOrWhiteSpace(me.DepartmentName)) departmentName = me.DepartmentName;
            if (!string.IsNullOrWhiteSpace(me.Position)) position = me.Position;
        }

        var empleado = await empleadoService.EnsureCurrentUserAsync(ct);

        var model = new TicketsIndexViewModel
        {
            Dashboard = await ticketService.GetDashboardAsync(ct),
            UserCode = currentUser.UserCode,
            FullName = fullName,
            DepartmentName = departmentName ?? "—",
            Position = position ?? "—",
            CurrentEmpleadoId = empleado.Id,
            CurrentEmpleadoNombre = empleado.Nombre
        };
        return View(model);
    }

    // ---------- Listado / detalle / dashboard ----------
    [HttpGet]
    public async Task<IActionResult> GetTickets([FromQuery] TicketFilterDto filter, CancellationToken ct)
        => Json(new { success = true, items = await ticketService.GetTicketsAsync(filter, ct) });

    [HttpGet]
    public async Task<IActionResult> GetTicket(int id, CancellationToken ct)
        => Json(new { success = true, data = await ticketService.GetDetailAsync(id, ct) });

    [HttpGet]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
        => Json(new { success = true, data = await ticketService.GetDashboardAsync(ct) });

    // ---------- Catálogos ----------
    [HttpGet]
    public async Task<IActionResult> GetClasificaciones(CancellationToken ct)
        => Json(new { success = true, data = await catalogService.GetClasificacionesAsync(ct) });

    [HttpGet]
    public async Task<IActionResult> GetCategorias(int clasificacionId, CancellationToken ct)
        => Json(new { success = true, data = await catalogService.GetCategoriasAsync(clasificacionId, ct) });

    [HttpGet]
    public async Task<IActionResult> GetPrioridades(CancellationToken ct)
        => Json(new { success = true, data = await catalogService.GetPrioridadesAsync(ct) });

    [HttpGet]
    public async Task<IActionResult> GetEstatus(CancellationToken ct)
        => Json(new { success = true, data = await catalogService.GetEstatusListAsync(ct) });

    [HttpPost]
    public async Task<IActionResult> AddClasificacion([FromForm] CreateClasificacionRequest request, CancellationToken ct)
        => Json(new { success = true, data = await catalogService.AddClasificacionAsync(request, ct) });

    [HttpPost]
    public async Task<IActionResult> AddCategoria([FromForm] CreateCategoriaRequest request, CancellationToken ct)
        => Json(new { success = true, data = await catalogService.AddCategoriaAsync(request, ct) });

    // ---------- Empleados (solicitantes) ----------
    [HttpGet]
    public async Task<IActionResult> GetEmpleados(CancellationToken ct)
        => Json(new { success = true, data = await empleadoService.GetAllAsync(ct) });

    [HttpPost]
    public async Task<IActionResult> AddEmpleado([FromForm] CreateEmpleadoRequest request, CancellationToken ct)
        => Json(new { success = true, data = await empleadoService.CreateAsync(request, ct) });

    // ---------- Tickets: alta / edición / estatus / responsable ----------
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateTicketRequest request, CancellationToken ct)
    {
        var id = await ticketService.CreateAsync(request, ct);
        return Json(new { success = true, message = "Ticket creado.", id });
    }

    [HttpPost]
    public async Task<IActionResult> Update([FromForm] UpdateTicketRequest request, CancellationToken ct)
    {
        await ticketService.UpdateAsync(request, ct);
        return Json(new { success = true, message = "Ticket actualizado." });
    }

    [HttpPost]
    public async Task<IActionResult> ChangeStatus([FromForm] ChangeStatusRequest request, CancellationToken ct)
    {
        await ticketService.ChangeStatusAsync(request, ct);
        return Json(new { success = true, message = "Estatus actualizado." });
    }

    [HttpPost]
    public async Task<IActionResult> AssignResponsable([FromForm] AssignResponsableRequest request, CancellationToken ct)
    {
        await ticketService.AssignResponsableAsync(request, ct);
        return Json(new { success = true, message = "Responsable asignado." });
    }

    [HttpPost]
    public async Task<IActionResult> SetInactive(int id, CancellationToken ct)
    {
        await ticketService.SetInactiveAsync(id, ct);
        return Json(new { success = true, message = "Ticket inactivado." });
    }

    // ---------- Comentarios ----------
    [HttpGet]
    public async Task<IActionResult> GetComments(int ticketId, CancellationToken ct)
        => Json(new { success = true, data = await commentService.GetByTicketAsync(ticketId, ct) });

    [HttpPost]
    public async Task<IActionResult> AddComment([FromForm] CreateCommentRequest request, CancellationToken ct)
    {
        var id = await commentService.AddAsync(request, ct);
        return Json(new { success = true, message = "Comentario agregado.", id });
    }

    // ---------- Historial y log ----------
    [HttpGet]
    public async Task<IActionResult> GetHistorial(int ticketId, CancellationToken ct)
        => Json(new { success = true, data = await ticketService.GetHistorialAsync(ticketId, ct) });

    [HttpGet]
    public async Task<IActionResult> GetLog(int ticketId, CancellationToken ct)
        => Json(new { success = true, data = await ticketService.GetLogAsync(ticketId, ct) });

    // ---------- Adjuntos ----------
    [HttpGet]
    public async Task<IActionResult> GetAdjuntos(int ticketId, CancellationToken ct)
        => Json(new { success = true, data = await adjuntoService.GetByTicketAsync(ticketId, ct) });

    [HttpPost]
    [RequestSizeLimit(60_000_000)]
    public async Task<IActionResult> UploadAdjuntos(int ticketId, List<IFormFile> files, CancellationToken ct)
    {
        if (files is null || files.Count == 0)
            return Json(new { success = false, message = "No se proporcionaron archivos." });

        var agregados = 0;
        var errores = new List<string>();

        foreach (var file in files)
        {
            try
            {
                var (nombreAlmacenado, tamano, tipo) = await fileStorage.SaveAsync(ticketId, file, ct);
                await adjuntoService.RecordAsync(ticketId, file.FileName, nombreAlmacenado, tipo, tamano, ct);
                agregados++;
            }
            catch (Application.Common.ValidationException ex)
            {
                errores.Add(ex.Message);
            }
        }

        return Json(new { success = errores.Count == 0, agregados, errores });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAdjunto(int id, CancellationToken ct)
    {
        var meta = await adjuntoService.GetForDownloadAsync(id, ct);
        if (meta is null) return NotFound();

        var file = fileStorage.Read(meta.Value.NombreAlmacenado);
        if (file is null) return NotFound();

        var contentType = meta.Value.TipoContenido;
        if (string.IsNullOrWhiteSpace(contentType))
        {
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(meta.Value.NombreOriginal, out contentType))
                contentType = "application/octet-stream";
        }
        return File(file.Value.Bytes, contentType, meta.Value.NombreOriginal);
    }
}
