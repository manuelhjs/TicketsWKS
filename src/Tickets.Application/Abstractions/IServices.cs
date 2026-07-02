using Tickets.Application.Common;
using Tickets.Application.Dtos;

namespace Tickets.Application.Abstractions;

public interface ITicketService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TicketListItemDto>> GetTicketsAsync(TicketFilterDto filter, CancellationToken ct = default);
    Task<TicketDetailDto> GetDetailAsync(int id, CancellationToken ct = default);

    Task<int> CreateAsync(CreateTicketRequest request, CancellationToken ct = default);
    Task UpdateAsync(UpdateTicketRequest request, CancellationToken ct = default);
    Task ChangeStatusAsync(ChangeStatusRequest request, CancellationToken ct = default);
    Task AssignResponsableAsync(AssignResponsableRequest request, CancellationToken ct = default);
    Task SetInactiveAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<HistorialEstatusDto>> GetHistorialAsync(int ticketId, CancellationToken ct = default);
    Task<IReadOnlyList<TicketLogDto>> GetLogAsync(int ticketId, CancellationToken ct = default);
}

public interface IEmpleadoService
{
    Task<IReadOnlyList<EmpleadoDto>> GetAllAsync(CancellationToken ct = default);
    Task<EmpleadoDto> CreateAsync(CreateEmpleadoRequest request, CancellationToken ct = default);

    /// <summary>Asegura que el usuario actual exista como Empleado (auto-alta) y lo devuelve.</summary>
    Task<EmpleadoDto> EnsureCurrentUserAsync(CancellationToken ct = default);
}

public interface ICatalogService
{
    Task<IReadOnlyList<SelectOption>> GetClasificacionesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SelectOption>> GetCategoriasAsync(int clasificacionId, CancellationToken ct = default);
    Task<IReadOnlyList<PrioridadDto>> GetPrioridadesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EstatusDto>> GetEstatusListAsync(CancellationToken ct = default);

    Task<SelectOption> AddClasificacionAsync(CreateClasificacionRequest request, CancellationToken ct = default);
    Task<SelectOption> AddCategoriaAsync(CreateCategoriaRequest request, CancellationToken ct = default);
}

public interface ITicketCommentService
{
    Task<IReadOnlyList<TicketCommentDto>> GetByTicketAsync(int ticketId, CancellationToken ct = default);
    Task<int> AddAsync(CreateCommentRequest request, CancellationToken ct = default);
}

public interface IAdjuntoService
{
    Task<IReadOnlyList<AdjuntoDto>> GetByTicketAsync(int ticketId, CancellationToken ct = default);

    /// <summary>Registra un adjunto ya guardado en disco. Valida el máximo por ticket y audita.</summary>
    Task<int> RecordAsync(int ticketId, string nombreOriginal, string nombreAlmacenado, string? tipoContenido, long tamanoBytes, CancellationToken ct = default);

    /// <summary>Datos para descargar un adjunto (nombre original y nombre almacenado en disco).</summary>
    Task<(string NombreOriginal, string NombreAlmacenado, string? TipoContenido)?> GetForDownloadAsync(int id, CancellationToken ct = default);
}
