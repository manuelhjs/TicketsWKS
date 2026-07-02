using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Tickets.Application.Abstractions;
using Tickets.Application.Dtos;
using Tickets.Web.Models;
using Tickets.Web.Services;

namespace Tickets.Web.Controllers;

/// <summary>
/// Controlador DELGADO: solo orquesta. Toda la lógica de negocio vive en los servicios;
/// los errores se gestionan en el middleware central (no hay try/catch por acción).
/// </summary>
public sealed class TicketsController(
    ITicketService ticketService,
    ITicketCommentService commentService,
    ICatalogService catalogService,
    ICurrentUserService currentUser,
    IUserDirectoryRepository userDirectory,
    IFileStorage fileStorage) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        // Datos de cabecera: se prefieren los del directorio SAP (nombre real y
        // nombre de departamento); si no está disponible, se usan los de configuración.
        var fullName = currentUser.FullName;
        var departmentName = currentUser.DepartmentName ?? currentUser.DepartmentCode;

        var dirUsers = await userDirectory.GetByCodesAsync([currentUser.UserCode], ct);
        var me = dirUsers.FirstOrDefault();
        if (me is not null)
        {
            if (!string.IsNullOrWhiteSpace(me.Name)) fullName = me.Name;
            if (!string.IsNullOrWhiteSpace(me.DepartmentName)) departmentName = me.DepartmentName;
        }

        var model = new TicketsIndexViewModel
        {
            Dashboard = await ticketService.GetDashboardAsync(ct),
            IsIt = currentUser.IsIt,
            UserCode = currentUser.UserCode,
            FullName = fullName,
            DepartmentName = departmentName ?? "—",
            Position = currentUser.Position ?? "—"
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetTickets([FromQuery] TicketFilterDto filter, CancellationToken ct)
    {
        var items = await ticketService.GetTicketsAsync(filter, ct);
        return Json(new { success = true, items });
    }

    [HttpGet]
    public async Task<IActionResult> GetTicket(int id, CancellationToken ct)
        => Json(new { success = true, data = await ticketService.GetDetailAsync(id, ct) });

    [HttpGet]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
        => Json(new { success = true, data = await ticketService.GetDashboardAsync(ct) });

    [HttpGet]
    public async Task<IActionResult> GetFilterOptions(CancellationToken ct)
        => Json(new { success = true, data = await ticketService.GetFilterOptionsAsync(ct) });

    [HttpGet]
    public async Task<IActionResult> GetAreas(CancellationToken ct)
        => Json(new { success = true, data = await catalogService.GetAreasAsync(ct) });

    [HttpGet]
    public async Task<IActionResult> GetTicketTypes(string areaCode, CancellationToken ct)
        => Json(new { success = true, data = await catalogService.GetTicketTypesByAreaAsync(areaCode, ct) });

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateTicketRequest request, CancellationToken ct)
    {
        var id = await ticketService.CreateAsync(request, ct);
        return Json(new { success = true, message = "Ticket creado.", id });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus([FromForm] UpdateTicketStatusRequest request, CancellationToken ct)
    {
        await ticketService.UpdateStatusAsync(request, ct);
        return Json(new { success = true, message = "Estatus actualizado." });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateEstimatedCloseDate([FromForm] UpdateEstimatedCloseDateRequest request, CancellationToken ct)
    {
        await ticketService.UpdateEstimatedCloseDateAsync(request, ct);
        return Json(new { success = true, message = "Fecha estimada actualizada." });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateResponsible([FromForm] UpdateResponsibleRequest request, CancellationToken ct)
    {
        await ticketService.UpdateResponsibleAsync(request, ct);
        return Json(new { success = true, message = "Responsable actualizado." });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateCategory([FromForm] UpdateCategoryRequest request, CancellationToken ct)
    {
        await ticketService.UpdateCategoryAsync(request, ct);
        return Json(new { success = true, message = "Tipo actualizado." });
    }

    [HttpPost]
    public async Task<IActionResult> SetInactive(int id, CancellationToken ct)
    {
        await ticketService.SetInactiveAsync(id, ct);
        return Json(new { success = true, message = "Ticket inactivado." });
    }

    [HttpGet]
    public async Task<IActionResult> GetComments(int ticketId, CancellationToken ct)
        => Json(new { success = true, data = await commentService.GetByTicketAsync(ticketId, ct) });

    [HttpPost]
    public async Task<IActionResult> AddComment([FromForm] CreateCommentRequest request, CancellationToken ct)
    {
        var id = await commentService.AddAsync(request, ct);
        return Json(new { success = true, message = "Comentario agregado.", id });
    }

    [HttpPost]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> UploadAttachment(int ticketId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Json(new { success = false, message = "No se proporcionó un archivo." });

        var fileName = await fileStorage.SaveTicketAttachmentAsync(ticketId, file, ct);
        await ticketService.SetAttachmentAsync(ticketId, fileName, ct);
        return Json(new { success = true, message = "Archivo adjuntado.", fileName });
    }

    [HttpGet]
    public IActionResult DownloadAttachment(string fileName)
    {
        var result = fileStorage.Read(fileName);
        if (result is null) return NotFound();

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(result.Value.FileName, out var contentType))
            contentType = "application/octet-stream";

        return File(result.Value.Bytes, contentType, result.Value.FileName);
    }
}
