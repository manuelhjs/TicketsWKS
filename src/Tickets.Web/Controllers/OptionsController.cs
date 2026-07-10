using Microsoft.AspNetCore.Mvc;
using Tickets.Application.Abstractions;
using Tickets.Application.Dtos;

namespace Tickets.Web.Controllers;

/// <summary>Administración de los catálogos que usa el alta de tickets. Controlador delgado.</summary>
public sealed class OptionsController(IOptionsAdminService options) : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    // ---------- Clasificaciones ----------
    [HttpGet]
    public async Task<IActionResult> GetClasificaciones(CancellationToken ct)
        => Json(new { success = true, data = await options.GetClasificacionesAsync(ct) });

    [HttpPost]
    public async Task<IActionResult> UpsertClasificacion([FromForm] UpsertClasificacionRequest request, CancellationToken ct)
        => Json(new { success = true, id = await options.UpsertClasificacionAsync(request, ct) });

    [HttpPost]
    public async Task<IActionResult> ToggleClasificacion(int id, bool activo, CancellationToken ct)
    {
        await options.SetClasificacionActivoAsync(id, activo, ct);
        return Json(new { success = true });
    }

    // ---------- Categorías ----------
    [HttpGet]
    public async Task<IActionResult> GetCategorias(CancellationToken ct)
        => Json(new { success = true, data = await options.GetCategoriasAsync(ct) });

    [HttpPost]
    public async Task<IActionResult> UpsertCategoria([FromForm] UpsertCategoriaRequest request, CancellationToken ct)
        => Json(new { success = true, id = await options.UpsertCategoriaAsync(request, ct) });

    [HttpPost]
    public async Task<IActionResult> ToggleCategoria(int id, bool activo, CancellationToken ct)
    {
        await options.SetCategoriaActivoAsync(id, activo, ct);
        return Json(new { success = true });
    }

    // ---------- Prioridades ----------
    [HttpGet]
    public async Task<IActionResult> GetPrioridades(CancellationToken ct)
        => Json(new { success = true, data = await options.GetPrioridadesAsync(ct) });

    [HttpPost]
    public async Task<IActionResult> UpsertPrioridad([FromForm] UpsertPrioridadRequest request, CancellationToken ct)
        => Json(new { success = true, id = await options.UpsertPrioridadAsync(request, ct) });

    [HttpPost]
    public async Task<IActionResult> TogglePrioridad(byte id, bool activo, CancellationToken ct)
    {
        await options.SetPrioridadActivoAsync(id, activo, ct);
        return Json(new { success = true });
    }

    // ---------- Estatus ----------
    [HttpGet]
    public async Task<IActionResult> GetEstatus(CancellationToken ct)
        => Json(new { success = true, data = await options.GetEstatusAsync(ct) });

    [HttpPost]
    public async Task<IActionResult> UpsertEstatus([FromForm] UpsertEstatusRequest request, CancellationToken ct)
        => Json(new { success = true, id = await options.UpsertEstatusAsync(request, ct) });

    [HttpPost]
    public async Task<IActionResult> ToggleEstatus(byte id, bool activo, CancellationToken ct)
    {
        await options.SetEstatusActivoAsync(id, activo, ct);
        return Json(new { success = true });
    }
}
