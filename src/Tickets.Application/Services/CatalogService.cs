using Tickets.Application.Abstractions;
using Tickets.Application.Common;
using Tickets.Application.Dtos;
using Tickets.Domain.Entities;

namespace Tickets.Application.Services;

public sealed class CatalogService(ICatalogRepository catalogRepository) : ICatalogService
{
    public async Task<IReadOnlyList<SelectOption>> GetClasificacionesAsync(CancellationToken ct = default)
    {
        var list = await catalogRepository.GetClasificacionesAsync(ct);
        return list.Select(c => new SelectOption { Value = c.Id.ToString(), Text = c.Nombre }).ToList();
    }

    public async Task<IReadOnlyList<SelectOption>> GetCategoriasAsync(int clasificacionId, CancellationToken ct = default)
    {
        var list = await catalogRepository.GetCategoriasByClasificacionAsync(clasificacionId, ct);
        return list.Select(c => new SelectOption { Value = c.Id.ToString(), Text = c.Nombre }).ToList();
    }

    public async Task<IReadOnlyList<PrioridadDto>> GetPrioridadesAsync(CancellationToken ct = default)
    {
        var list = await catalogRepository.GetPrioridadesAsync(ct);
        return list.Select(p => new PrioridadDto { Id = p.Id, Nombre = p.Nombre, Descripcion = p.Descripcion }).ToList();
    }

    public async Task<IReadOnlyList<EstatusDto>> GetEstatusListAsync(CancellationToken ct = default)
    {
        var list = await catalogRepository.GetEstatusListAsync(ct);
        return list.Select(e => new EstatusDto { Id = e.Id, Nombre = e.Nombre, EsFinal = e.EsFinal }).ToList();
    }

    public async Task<SelectOption> AddClasificacionAsync(CreateClasificacionRequest request, CancellationToken ct = default)
    {
        var nombre = (request.Nombre ?? "").Trim();
        if (nombre.Length == 0)
            throw new ValidationException("El nombre de la clasificación es obligatorio.");

        var existing = await catalogRepository.GetClasificacionByNombreAsync(nombre, ct);
        if (existing is not null)
            return new SelectOption { Value = existing.Id.ToString(), Text = existing.Nombre };

        var clasificacion = new Clasificacion { Nombre = nombre, Activo = true, FechaAlta = DateTime.UtcNow };
        clasificacion.Id = await catalogRepository.InsertClasificacionAsync(clasificacion, ct);
        return new SelectOption { Value = clasificacion.Id.ToString(), Text = clasificacion.Nombre };
    }

    public async Task<SelectOption> AddCategoriaAsync(CreateCategoriaRequest request, CancellationToken ct = default)
    {
        var nombre = (request.Nombre ?? "").Trim();
        if (nombre.Length == 0)
            throw new ValidationException("El nombre de la categoría es obligatorio.");

        _ = await catalogRepository.GetClasificacionAsync(request.ClasificacionId, ct)
            ?? throw new ValidationException("La clasificación padre no existe.");

        var existing = await catalogRepository.GetCategoriaByNombreAsync(request.ClasificacionId, nombre, ct);
        if (existing is not null)
            return new SelectOption { Value = existing.Id.ToString(), Text = existing.Nombre };

        var categoria = new Categoria
        {
            ClasificacionId = request.ClasificacionId,
            Nombre = nombre,
            Activo = true,
            FechaAlta = DateTime.UtcNow
        };
        categoria.Id = await catalogRepository.InsertCategoriaAsync(categoria, ct);
        return new SelectOption { Value = categoria.Id.ToString(), Text = categoria.Nombre };
    }
}
