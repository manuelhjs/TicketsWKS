using Tickets.Domain.Entities;

namespace Tickets.Application.Abstractions;

public interface ICatalogRepository
{
    Task<IReadOnlyList<Area>> GetActiveAreasAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TicketType>> GetTicketTypesByAreaAsync(string areaCode, CancellationToken ct = default);
    Task<IReadOnlyList<TicketType>> GetActiveTicketTypesAsync(CancellationToken ct = default);
    Task<TicketType?> GetTicketTypeAsync(int id, CancellationToken ct = default);
}
