using Dapper;
using Tickets.Application.Abstractions;
using Tickets.Domain.Entities;
using Tickets.Infrastructure.Persistence;

namespace Tickets.Infrastructure.Repositories;

public sealed class CatalogRepository(ISqlConnectionFactory connectionFactory) : ICatalogRepository
{
    public async Task<IReadOnlyList<Area>> GetActiveAreasAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT Id, Code, Name, IsActive, CreatedAt FROM dbo.Areas WHERE IsActive = 1 ORDER BY Name;";
        await using var conn = connectionFactory.CreateTicketsConnection();
        return (await conn.QueryAsync<Area>(new CommandDefinition(sql, cancellationToken: ct))).ToList();
    }

    public async Task<IReadOnlyList<TicketType>> GetTicketTypesByAreaAsync(string areaCode, CancellationToken ct = default)
    {
        const string sql = """
            SELECT tt.Id, tt.AreaId, tt.Name, tt.DefaultResponsibleUserCode, tt.IsActive, tt.CreatedAt,
                   a.Code AS AreaCode, a.Name AS AreaName
            FROM dbo.TicketTypes tt
            INNER JOIN dbo.Areas a ON a.Id = tt.AreaId
            WHERE tt.IsActive = 1 AND a.Code = @AreaCode
            ORDER BY tt.Name;
            """;
        await using var conn = connectionFactory.CreateTicketsConnection();
        return (await conn.QueryAsync<TicketType>(new CommandDefinition(sql, new { AreaCode = areaCode }, cancellationToken: ct))).ToList();
    }

    public async Task<IReadOnlyList<TicketType>> GetActiveTicketTypesAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT tt.Id, tt.AreaId, tt.Name, tt.DefaultResponsibleUserCode, tt.IsActive, tt.CreatedAt,
                   a.Code AS AreaCode, a.Name AS AreaName
            FROM dbo.TicketTypes tt
            INNER JOIN dbo.Areas a ON a.Id = tt.AreaId
            WHERE tt.IsActive = 1
            ORDER BY tt.Name;
            """;
        await using var conn = connectionFactory.CreateTicketsConnection();
        return (await conn.QueryAsync<TicketType>(new CommandDefinition(sql, cancellationToken: ct))).ToList();
    }

    public async Task<TicketType?> GetTicketTypeAsync(int id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT tt.Id, tt.AreaId, tt.Name, tt.DefaultResponsibleUserCode, tt.IsActive, tt.CreatedAt,
                   a.Code AS AreaCode, a.Name AS AreaName
            FROM dbo.TicketTypes tt
            INNER JOIN dbo.Areas a ON a.Id = tt.AreaId
            WHERE tt.Id = @Id;
            """;
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleOrDefaultAsync<TicketType>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }
}
