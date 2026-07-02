using Tickets.Application.Dtos;
using Tickets.Domain.Entities;

namespace Tickets.Application.Abstractions;

public interface ITicketRepository
{
    Task<IReadOnlyList<TicketListItemDto>> GetListAsync(TicketFilterDto filter, int maxRows, CancellationToken ct = default);
    Task<TicketDetailDto?> GetDetailAsync(int id, CancellationToken ct = default);
    Task<Ticket?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> InsertAsync(Ticket ticket, CancellationToken ct = default);
    Task<bool> UpdateFieldsAsync(Ticket ticket, CancellationToken ct = default);
    Task<bool> UpdateEstatusAsync(int id, byte estatusId, DateTime? closedAt, CancellationToken ct = default);
    Task<bool> UpdateResponsableAsync(int id, int responsableEmpleadoId, CancellationToken ct = default);
    Task<bool> SetInactiveAsync(int id, CancellationToken ct = default);
    Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default);
}
