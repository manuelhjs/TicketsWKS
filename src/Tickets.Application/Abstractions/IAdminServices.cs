using Tickets.Application.Dtos;

namespace Tickets.Application.Abstractions;

/// <summary>Administración de los catálogos que usa el alta de tickets.</summary>
public interface IOptionsAdminService
{
    // Clasificaciones
    Task<IReadOnlyList<ClasificacionAdminDto>> GetClasificacionesAsync(CancellationToken ct = default);
    Task<int> UpsertClasificacionAsync(UpsertClasificacionRequest request, CancellationToken ct = default);
    Task SetClasificacionActivoAsync(int id, bool activo, CancellationToken ct = default);

    // Categorías
    Task<IReadOnlyList<CategoriaAdminDto>> GetCategoriasAsync(CancellationToken ct = default);
    Task<int> UpsertCategoriaAsync(UpsertCategoriaRequest request, CancellationToken ct = default);
    Task SetCategoriaActivoAsync(int id, bool activo, CancellationToken ct = default);

    // Prioridades
    Task<IReadOnlyList<PrioridadAdminDto>> GetPrioridadesAsync(CancellationToken ct = default);
    Task<byte> UpsertPrioridadAsync(UpsertPrioridadRequest request, CancellationToken ct = default);
    Task SetPrioridadActivoAsync(byte id, bool activo, CancellationToken ct = default);

    // Estatus
    Task<IReadOnlyList<EstatusAdminDto>> GetEstatusAsync(CancellationToken ct = default);
    Task<byte> UpsertEstatusAsync(UpsertEstatusRequest request, CancellationToken ct = default);
    Task SetEstatusActivoAsync(byte id, bool activo, CancellationToken ct = default);
}

/// <summary>Administración de empleados (reutiliza la tabla de Solicitante).</summary>
public interface IEmpleadoAdminService
{
    Task<IReadOnlyList<EmpleadoDto>> GetAllAsync(CancellationToken ct = default);
    Task<int> UpsertAsync(EmpleadoFormRequest request, CancellationToken ct = default);
    Task SetActivoAsync(int id, bool activo, CancellationToken ct = default);

    /// <summary>Exporta todos los empleados en CSV (UTF-8 con BOM, compatible con Excel).</summary>
    Task<byte[]> ExportCsvAsync(CancellationToken ct = default);

    /// <summary>Importa empleados desde CSV. Upsert por Código o Nombre+Correo; filas inválidas se omiten.</summary>
    Task<ImportEmpleadosResultDto> ImportCsvAsync(string csvContent, CancellationToken ct = default);
}
