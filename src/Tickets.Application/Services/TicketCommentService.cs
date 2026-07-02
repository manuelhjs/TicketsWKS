using Tickets.Application.Abstractions;
using Tickets.Application.Common;
using Tickets.Application.Dtos;
using Tickets.Domain.Entities;

namespace Tickets.Application.Services;

public sealed class TicketCommentService(
    ITicketCommentRepository commentRepository,
    ITicketRepository ticketRepository,
    ITicketLogRepository logRepository,
    IUserDirectoryRepository userDirectory,
    ICurrentUserService currentUser) : ITicketCommentService
{
    public async Task<IReadOnlyList<TicketCommentDto>> GetByTicketAsync(int ticketId, CancellationToken ct = default)
    {
        var comments = await commentRepository.GetByTicketAsync(ticketId, ct);
        if (comments.Count == 0) return [];

        var names = await LoadNamesAsync(comments.Select(c => c.AutorCodigo), ct);

        return comments.Select(c => new TicketCommentDto
        {
            Id = c.Id,
            TicketId = c.TicketId,
            AutorCodigo = c.AutorCodigo,
            AutorNombre = names.TryGetValue(c.AutorCodigo, out var name) ? name : c.AutorCodigo,
            Comentario = c.Comentario,
            CreatedAt = c.CreatedAt
        }).ToList();
    }

    public async Task<int> AddAsync(CreateCommentRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Comentario))
            throw new ValidationException("El comentario no puede estar vacío.");

        _ = await ticketRepository.GetByIdAsync(request.TicketId, ct)
            ?? throw new NotFoundException($"Ticket {request.TicketId} no encontrado.");

        var comment = new TicketComment
        {
            TicketId = request.TicketId,
            AutorCodigo = currentUser.UserCode,
            Comentario = request.Comentario.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        var id = await commentRepository.InsertAsync(comment, ct);

        await logRepository.InsertAsync(new TicketLogEntry
        {
            TicketId = request.TicketId,
            Accion = "Comentario",
            Descripcion = "Comentario agregado",
            UsuarioCodigo = currentUser.UserCode,
            FechaHora = DateTime.UtcNow
        }, ct);

        return id;
    }

    private async Task<Dictionary<string, string>> LoadNamesAsync(IEnumerable<string> codes, CancellationToken ct)
    {
        var distinct = codes.Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (distinct.Count == 0) return map;

        foreach (var u in await userDirectory.GetByCodesAsync(distinct, ct))
            map[u.Code] = string.IsNullOrWhiteSpace(u.Name) ? u.Code : u.Name;
        return map;
    }
}
