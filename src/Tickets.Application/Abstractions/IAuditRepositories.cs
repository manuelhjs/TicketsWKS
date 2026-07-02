using Tickets.Application.Dtos;
using Tickets.Domain.Entities;

namespace Tickets.Application.Abstractions;

public interface IHistorialEstatusRepository
{
    Task<int> InsertAsync(HistorialEstatus historial, CancellationToken ct = default);
    Task<IReadOnlyList<HistorialEstatusDto>> GetByTicketAsync(int ticketId, CancellationToken ct = default);
}

public interface IAdjuntoRepository
{
    Task<int> InsertAsync(Adjunto adjunto, CancellationToken ct = default);
    Task<IReadOnlyList<Adjunto>> GetByTicketAsync(int ticketId, CancellationToken ct = default);
    Task<int> CountByTicketAsync(int ticketId, CancellationToken ct = default);
    Task<Adjunto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

/// <summary>Auditoría general. Solo inserción y lectura (nunca update/delete).</summary>
public interface ITicketLogRepository
{
    Task InsertAsync(TicketLogEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<TicketLogEntry>> GetByTicketAsync(int ticketId, CancellationToken ct = default);
}
