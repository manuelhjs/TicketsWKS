using Tickets.Application.Common;
using Tickets.Application.Dtos;

namespace Tickets.Application.Abstractions;

public interface ITicketService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default);

    /// <summary>Lista filtrada según rol/visibilidad. DataTables la pagina en cliente.</summary>
    Task<IReadOnlyList<TicketListItemDto>> GetTicketsAsync(TicketFilterDto filter, CancellationToken ct = default);

    /// <summary>Detalle para el modal, con validación de propiedad/estatus según rol.</summary>
    Task<TicketListItemDto> GetDetailAsync(int id, CancellationToken ct = default);

    Task<FilterOptionsDto> GetFilterOptionsAsync(CancellationToken ct = default);

    Task<int> CreateAsync(CreateTicketRequest request, CancellationToken ct = default);
    Task UpdateStatusAsync(UpdateTicketStatusRequest request, CancellationToken ct = default);
    Task UpdateEstimatedCloseDateAsync(UpdateEstimatedCloseDateRequest request, CancellationToken ct = default);
    Task UpdateResponsibleAsync(UpdateResponsibleRequest request, CancellationToken ct = default);
    Task UpdateCategoryAsync(UpdateCategoryRequest request, CancellationToken ct = default);
    Task SetInactiveAsync(int id, CancellationToken ct = default);
    Task SetAttachmentAsync(int id, string fileName, CancellationToken ct = default);
}

public interface ITicketCommentService
{
    Task<IReadOnlyList<TicketCommentDto>> GetByTicketAsync(int ticketId, CancellationToken ct = default);
    Task<int> AddAsync(CreateCommentRequest request, CancellationToken ct = default);
}

public interface ICatalogService
{
    Task<IReadOnlyList<SelectOption>> GetAreasAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TicketTypeDto>> GetTicketTypesByAreaAsync(string areaCode, CancellationToken ct = default);
}
