using System.Text;
using Microsoft.AspNetCore.Mvc;
using Tickets.Application.Abstractions;
using Tickets.Application.Dtos;

namespace Tickets.Web.Controllers;

/// <summary>Módulo de Empleados (reutiliza la tabla de Solicitante). Controlador delgado.</summary>
public sealed class EmpleadosController(IEmpleadoAdminService empleados) : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> GetEmpleados(CancellationToken ct)
        => Json(new { success = true, data = await empleados.GetAllAsync(ct) });

    [HttpPost]
    public async Task<IActionResult> Upsert([FromForm] EmpleadoFormRequest request, CancellationToken ct)
        => Json(new { success = true, id = await empleados.UpsertAsync(request, ct) });

    [HttpPost]
    public async Task<IActionResult> Toggle(int id, bool activo, CancellationToken ct)
    {
        await empleados.SetActivoAsync(id, activo, ct);
        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var bytes = await empleados.ExportCsvAsync(ct);
        return File(bytes, "text/csv", $"empleados_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpPost]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Json(new { success = false, message = "No se proporcionó un archivo." });

        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var content = await reader.ReadToEndAsync(ct);

        var result = await empleados.ImportCsvAsync(content, ct);
        return Json(new { success = true, data = result });
    }
}
