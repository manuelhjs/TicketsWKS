using Dapper;
using Tickets.Application.Abstractions;
using Tickets.Domain.Entities;
using Tickets.Infrastructure.Persistence;

namespace Tickets.Infrastructure.Repositories;

public sealed class TicketCommentRepository(ISqlConnectionFactory connectionFactory) : ITicketCommentRepository
{
    public async Task<IReadOnlyList<TicketComment>> GetByTicketAsync(int ticketId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, TicketId, AuthorUserCode, Body, CreatedAt
            FROM dbo.TicketComments
            WHERE TicketId = @TicketId
            ORDER BY CreatedAt;
            """;

        await using var conn = connectionFactory.CreateTicketsConnection();
        var rows = await conn.QueryAsync<TicketComment>(new CommandDefinition(sql, new { TicketId = ticketId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<int> InsertAsync(TicketComment comment, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.TicketComments (TicketId, AuthorUserCode, Body, CreatedAt)
            VALUES (@TicketId, @AuthorUserCode, @Body, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleAsync<int>(new CommandDefinition(sql, comment, cancellationToken: ct));
    }
}
