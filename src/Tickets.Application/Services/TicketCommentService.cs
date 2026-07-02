using Tickets.Application.Abstractions;
using Tickets.Application.Common;
using Tickets.Application.Dtos;
using Tickets.Domain.Entities;

namespace Tickets.Application.Services;

public sealed class TicketCommentService(
    ITicketCommentRepository commentRepository,
    ITicketRepository ticketRepository,
    IUserDirectoryRepository userDirectory,
    ICurrentUserService currentUser) : ITicketCommentService
{
    public async Task<IReadOnlyList<TicketCommentDto>> GetByTicketAsync(int ticketId, CancellationToken ct = default)
    {
        var comments = await commentRepository.GetByTicketAsync(ticketId, ct);
        if (comments.Count == 0) return [];

        var directory = await LoadNamesAsync(comments.Select(c => c.AuthorUserCode), ct);

        return comments.Select(c => new TicketCommentDto
        {
            Id = c.Id,
            TicketId = c.TicketId,
            AuthorUserCode = c.AuthorUserCode,
            AuthorName = directory.TryGetValue(c.AuthorUserCode, out var name) ? name : c.AuthorUserCode,
            Body = c.Body,
            CreatedAt = c.CreatedAt
        }).ToList();
    }

    public async Task<int> AddAsync(CreateCommentRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
            throw new ValidationException("El comentario no puede estar vacío.");

        _ = await ticketRepository.GetByIdAsync(request.TicketId, ct)
            ?? throw new NotFoundException($"Ticket {request.TicketId} no encontrado.");

        var comment = new TicketComment
        {
            TicketId = request.TicketId,
            AuthorUserCode = currentUser.UserCode,
            Body = request.Body.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        return await commentRepository.InsertAsync(comment, ct);
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
