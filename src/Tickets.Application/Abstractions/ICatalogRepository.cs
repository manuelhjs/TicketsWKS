using Tickets.Domain.Entities;

namespace Tickets.Application.Abstractions;

public interface ICatalogRepository
{
    // Clasificación
    Task<IReadOnlyList<Clasificacion>> GetClasificacionesAsync(CancellationToken ct = default);
    Task<Clasificacion?> GetClasificacionAsync(int id, CancellationToken ct = default);
    Task<Clasificacion?> GetClasificacionByNombreAsync(string nombre, CancellationToken ct = default);
    Task<int> InsertClasificacionAsync(Clasificacion clasificacion, CancellationToken ct = default);

    // Categoría
    Task<IReadOnlyList<Categoria>> GetCategoriasByClasificacionAsync(int clasificacionId, CancellationToken ct = default);
    Task<Categoria?> GetCategoriaAsync(int id, CancellationToken ct = default);
    Task<Categoria?> GetCategoriaByNombreAsync(int clasificacionId, string nombre, CancellationToken ct = default);
    Task<int> InsertCategoriaAsync(Categoria categoria, CancellationToken ct = default);

    // Catálogos fijos
    Task<IReadOnlyList<Prioridad>> GetPrioridadesAsync(CancellationToken ct = default);
    Task<Prioridad?> GetPrioridadAsync(byte id, CancellationToken ct = default);
    Task<IReadOnlyList<Estatus>> GetEstatusListAsync(CancellationToken ct = default);
    Task<Estatus?> GetEstatusAsync(byte id, CancellationToken ct = default);
}
