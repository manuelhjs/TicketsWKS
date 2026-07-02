using Tickets.Application.Common;
using Tickets.Application.Dtos;
using Tickets.Domain.Entities;
using Tickets.Domain.Enums;

namespace Tickets.Application.Abstractions;

public interface ITicketRepository
{
    Task<PagedResult<TicketListItemDto>> QueryAsync(TicketQuery query, CancellationToken ct = default);

    Task<DashboardDto> GetDashboardAsync(TicketQuery query, CancellationToken ct = default);

    Task<Ticket?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<int> InsertAsync(Ticket ticket, CancellationToken ct = default);

    Task<bool> UpdateStatusAsync(
        int id, TicketStatus status, DateTime? closedAt, TimeOnly? closedTime,
        DateOnly? estimatedCloseDate, CancellationToken ct = default);

    Task<bool> UpdateEstimatedCloseDateAsync(int id, DateOnly date, CancellationToken ct = default);

    Task<bool> UpdateResponsibleAsync(int id, string responsibleUserCode, CancellationToken ct = default);

    Task<bool> UpdateCategoryAsync(int id, string category, CancellationToken ct = default);

    Task<bool> SetInactiveAsync(int id, CancellationToken ct = default);

    Task<bool> SetAttachmentAsync(int id, string fileName, CancellationToken ct = default);

    /// <summary>Códigos distintos (solicitantes / departamentos / responsables) presentes en tickets visibles.</summary>
    Task<IReadOnlyList<string>> GetDistinctRequesterCodesAsync(TicketQuery visibility, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetDistinctDepartmentCodesAsync(TicketQuery visibility, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetDistinctResponsibleCodesAsync(TicketQuery visibility, CancellationToken ct = default);
}
