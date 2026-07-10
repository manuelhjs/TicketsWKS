using Tickets.Domain.Entities;

namespace Tickets.Application.Abstractions;

public interface ICatalogRepository
{
    // ---------- Clasificación ----------
    Task<IReadOnlyList<Clasificacion>> GetClasificacionesAsync(CancellationToken ct = default);      // solo activas
    Task<IReadOnlyList<Clasificacion>> GetAllClasificacionesAsync(CancellationToken ct = default);   // incluye inactivas
    Task<Clasificacion?> GetClasificacionAsync(int id, CancellationToken ct = default);
    Task<Clasificacion?> GetClasificacionByNombreAsync(string nombre, CancellationToken ct = default);
    Task<int> InsertClasificacionAsync(Clasificacion clasificacion, CancellationToken ct = default);
    Task<bool> UpdateClasificacionAsync(Clasificacion clasificacion, CancellationToken ct = default);
    Task<bool> SetClasificacionActivoAsync(int id, bool activo, CancellationToken ct = default);

    // ---------- Categoría ----------
    Task<IReadOnlyList<Categoria>> GetCategoriasByClasificacionAsync(int clasificacionId, CancellationToken ct = default); // solo activas
    Task<IReadOnlyList<Categoria>> GetAllCategoriasAsync(CancellationToken ct = default);            // incluye inactivas
    Task<Categoria?> GetCategoriaAsync(int id, CancellationToken ct = default);
    Task<Categoria?> GetCategoriaByNombreAsync(int clasificacionId, string nombre, CancellationToken ct = default);
    Task<int> InsertCategoriaAsync(Categoria categoria, CancellationToken ct = default);
    Task<bool> UpdateCategoriaAsync(Categoria categoria, CancellationToken ct = default);
    Task<bool> SetCategoriaActivoAsync(int id, bool activo, CancellationToken ct = default);

    // ---------- Prioridad ----------
    Task<IReadOnlyList<Prioridad>> GetPrioridadesAsync(CancellationToken ct = default);              // solo activas
    Task<IReadOnlyList<Prioridad>> GetAllPrioridadesAsync(CancellationToken ct = default);
    Task<Prioridad?> GetPrioridadAsync(byte id, CancellationToken ct = default);
    Task<byte> InsertPrioridadAsync(Prioridad prioridad, CancellationToken ct = default);            // asigna Id = MAX+1
    Task<bool> UpdatePrioridadAsync(Prioridad prioridad, CancellationToken ct = default);
    Task<bool> SetPrioridadActivoAsync(byte id, bool activo, CancellationToken ct = default);

    // ---------- Estatus ----------
    Task<IReadOnlyList<Estatus>> GetEstatusListAsync(CancellationToken ct = default);                // solo activos
    Task<IReadOnlyList<Estatus>> GetAllEstatusAsync(CancellationToken ct = default);
    Task<Estatus?> GetEstatusAsync(byte id, CancellationToken ct = default);
    Task<byte> InsertEstatusAsync(Estatus estatus, CancellationToken ct = default);
    Task<bool> UpdateEstatusAsync(Estatus estatus, CancellationToken ct = default);
    Task<bool> SetEstatusActivoAsync(byte id, bool activo, CancellationToken ct = default);
}
