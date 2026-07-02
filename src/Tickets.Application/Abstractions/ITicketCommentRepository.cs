using Tickets.Domain.Entities;

namespace Tickets.Application.Abstractions;

public interface ITicketCommentRepository
{
    Task<IReadOnlyList<TicketComment>> GetByTicketAsync(int ticketId, CancellationToken ct = default);
    Task<int> InsertAsync(TicketComment comment, CancellationToken ct = default);
}
