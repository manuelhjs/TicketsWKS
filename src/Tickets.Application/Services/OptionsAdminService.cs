using Tickets.Application.Abstractions;
using Tickets.Application.Common;
using Tickets.Application.Dtos;
using Tickets.Domain.Entities;

namespace Tickets.Application.Services;

public sealed class OptionsAdminService(ICatalogRepository catalog) : IOptionsAdminService
{
    // ---------- Clasificaciones ----------
    public async Task<IReadOnlyList<ClasificacionAdminDto>> GetClasificacionesAsync(CancellationToken ct = default)
    {
        var list = await catalog.GetAllClasificacionesAsync(ct);
        return list.Select(c => new ClasificacionAdminDto { Id = c.Id, Nombre = c.Nombre, Activo = c.Activo }).ToList();
    }

    public async Task<int> UpsertClasificacionAsync(UpsertClasificacionRequest r, CancellationToken ct = default)
    {
        var nombre = Require(r.Nombre, "El nombre de la clasificación es obligatorio.");
        // Validación robusta (case-insensitive, sin depender de la collation de la BD).
        var todas = await catalog.GetAllClasificacionesAsync(ct);
        if (todas.Any(c => c.Id != r.Id && string.Equals(c.Nombre?.Trim(), nombre, StringComparison.OrdinalIgnoreCase)))
            throw new ValidationException("Ya existe una clasificación con ese nombre.");

        if (r.Id == 0)
        {
            var entity = new Clasificacion { Nombre = nombre, Activo = r.Activo, FechaAlta = DateTime.UtcNow };
            return await catalog.InsertClasificacionAsync(entity, ct);
        }
        var existing = await catalog.GetClasificacionAsync(r.Id, ct)
            ?? throw new NotFoundException("Clasificación no encontrada.");
        existing.Nombre = nombre; existing.Activo = r.Activo;
        await catalog.UpdateClasificacionAsync(existing, ct);
        return existing.Id;
    }

    public Task SetClasificacionActivoAsync(int id, bool activo, CancellationToken ct = default)
        => catalog.SetClasificacionActivoAsync(id, activo, ct);

    // ---------- Categorías ----------
    public async Task<IReadOnlyList<CategoriaAdminDto>> GetCategoriasAsync(CancellationToken ct = default)
    {
        var cats = await catalog.GetAllCategoriasAsync(ct);
        var clasifs = (await catalog.GetAllClasificacionesAsync(ct)).ToDictionary(c => c.Id, c => c.Nombre);
        return cats.Select(c => new CategoriaAdminDto
        {
            Id = c.Id,
            ClasificacionId = c.ClasificacionId,
            ClasificacionNombre = clasifs.TryGetValue(c.ClasificacionId, out var n) ? n : "",
            Nombre = c.Nombre,
            Activo = c.Activo
        }).OrderBy(c => c.ClasificacionNombre).ThenBy(c => c.Nombre).ToList();
    }

    public async Task<int> UpsertCategoriaAsync(UpsertCategoriaRequest r, CancellationToken ct = default)
    {
        var nombre = Require(r.Nombre, "El nombre de la categoría es obligatorio.");
        _ = await catalog.GetClasificacionAsync(r.ClasificacionId, ct)
            ?? throw new ValidationException("La clasificación padre no existe.");

        // Validación robusta (case-insensitive) dentro de la misma clasificación.
        var todas = await catalog.GetAllCategoriasAsync(ct);
        if (todas.Any(c => c.Id != r.Id && c.ClasificacionId == r.ClasificacionId
                && string.Equals(c.Nombre?.Trim(), nombre, StringComparison.OrdinalIgnoreCase)))
            throw new ValidationException("Ya existe una categoría con ese nombre en la clasificación.");

        if (r.Id == 0)
        {
            var entity = new Categoria { ClasificacionId = r.ClasificacionId, Nombre = nombre, Activo = r.Activo, FechaAlta = DateTime.UtcNow };
            return await catalog.InsertCategoriaAsync(entity, ct);
        }
        var existing = await catalog.GetCategoriaAsync(r.Id, ct)
            ?? throw new NotFoundException("Categoría no encontrada.");
        existing.ClasificacionId = r.ClasificacionId; existing.Nombre = nombre; existing.Activo = r.Activo;
        await catalog.UpdateCategoriaAsync(existing, ct);
        return existing.Id;
    }

    public Task SetCategoriaActivoAsync(int id, bool activo, CancellationToken ct = default)
        => catalog.SetCategoriaActivoAsync(id, activo, ct);

    // ---------- Prioridades ----------
    public async Task<IReadOnlyList<PrioridadAdminDto>> GetPrioridadesAsync(CancellationToken ct = default)
    {
        var list = await catalog.GetAllPrioridadesAsync(ct);
        return list.Select(p => new PrioridadAdminDto { Id = p.Id, Nombre = p.Nombre, Descripcion = p.Descripcion, Orden = p.Orden, Activo = p.Activo }).ToList();
    }

    public async Task<byte> UpsertPrioridadAsync(UpsertPrioridadRequest r, CancellationToken ct = default)
    {
        var nombre = Require(r.Nombre, "El nombre de la prioridad es obligatorio.");
        if (r.Id == 0)
        {
            var entity = new Prioridad { Nombre = nombre, Descripcion = r.Descripcion ?? "", Orden = r.Orden, Activo = r.Activo };
            return await catalog.InsertPrioridadAsync(entity, ct);
        }
        var existing = await catalog.GetPrioridadAsync(r.Id, ct)
            ?? throw new NotFoundException("Prioridad no encontrada.");
        existing.Nombre = nombre; existing.Descripcion = r.Descripcion ?? ""; existing.Orden = r.Orden; existing.Activo = r.Activo;
        await catalog.UpdatePrioridadAsync(existing, ct);
        return existing.Id;
    }

    public Task SetPrioridadActivoAsync(byte id, bool activo, CancellationToken ct = default)
        => catalog.SetPrioridadActivoAsync(id, activo, ct);

    // ---------- Estatus ----------
    public async Task<IReadOnlyList<EstatusAdminDto>> GetEstatusAsync(CancellationToken ct = default)
    {
        var list = await catalog.GetAllEstatusAsync(ct);
        return list.Select(e => new EstatusAdminDto { Id = e.Id, Nombre = e.Nombre, Orden = e.Orden, EsFinal = e.EsFinal, Activo = e.Activo }).ToList();
    }

    public async Task<byte> UpsertEstatusAsync(UpsertEstatusRequest r, CancellationToken ct = default)
    {
        var nombre = Require(r.Nombre, "El nombre del estatus es obligatorio.");
        var todos = await catalog.GetAllEstatusAsync(ct);
        if (todos.Any(e => e.Id != r.Id && string.Equals(e.Nombre, nombre, StringComparison.OrdinalIgnoreCase)))
            throw new ValidationException("Ya existe un estatus con ese nombre.");

        if (r.Id == 0)
        {
            var entity = new Estatus { Nombre = nombre, Orden = r.Orden, EsFinal = r.EsFinal, Activo = r.Activo };
            return await catalog.InsertEstatusAsync(entity, ct);
        }
        var existing = await catalog.GetEstatusAsync(r.Id, ct)
            ?? throw new NotFoundException("Estatus no encontrado.");
        existing.Nombre = nombre; existing.Orden = r.Orden; existing.EsFinal = r.EsFinal; existing.Activo = r.Activo;
        await catalog.UpdateEstatusAsync(existing, ct);
        return existing.Id;
    }

    public Task SetEstatusActivoAsync(byte id, bool activo, CancellationToken ct = default)
        => catalog.SetEstatusActivoAsync(id, activo, ct);

    private static string Require(string? value, string message)
        => string.IsNullOrWhiteSpace(value) ? throw new ValidationException(message) : value.Trim();
}
