using Dapper;
using Tickets.Application.Abstractions;
using Tickets.Application.Dtos;
using Tickets.Domain.Entities;
using Tickets.Infrastructure.Persistence;

namespace Tickets.Infrastructure.Repositories;

public sealed class HistorialEstatusRepository(ISqlConnectionFactory connectionFactory) : IHistorialEstatusRepository
{
    public async Task<int> InsertAsync(HistorialEstatus h, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.HistorialEstatus (TicketId, EstatusAnteriorId, EstatusNuevoId, Comentario, UsuarioCodigo, Fecha)
            VALUES (@TicketId, @EstatusAnteriorId, @EstatusNuevoId, @Comentario, @UsuarioCodigo, @Fecha);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleAsync<int>(new CommandDefinition(sql, h, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<HistorialEstatusDto>> GetByTicketAsync(int ticketId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT h.Id, ea.Nombre AS EstatusAnterior, en.Nombre AS EstatusNuevo,
                   h.Comentario, h.UsuarioCodigo, h.Fecha
            FROM dbo.HistorialEstatus h
            LEFT JOIN dbo.Estatus ea ON ea.Id = h.EstatusAnteriorId
            INNER JOIN dbo.Estatus en ON en.Id = h.EstatusNuevoId
            WHERE h.TicketId = @TicketId
            ORDER BY h.Fecha, h.Id;
            """;
        await using var conn = connectionFactory.CreateTicketsConnection();
        return (await conn.QueryAsync<HistorialEstatusDto>(new CommandDefinition(sql, new { TicketId = ticketId }, cancellationToken: ct))).ToList();
    }
}

public sealed class AdjuntoRepository(ISqlConnectionFactory connectionFactory) : IAdjuntoRepository
{
    private const string Columns = "Id, TicketId, NombreOriginal, NombreAlmacenado, TipoContenido, TamanoBytes, UsuarioCodigo, Fecha";

    public async Task<int> InsertAsync(Adjunto a, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.Adjuntos (TicketId, NombreOriginal, NombreAlmacenado, TipoContenido, TamanoBytes, UsuarioCodigo, Fecha)
            VALUES (@TicketId, @NombreOriginal, @NombreAlmacenado, @TipoContenido, @TamanoBytes, @UsuarioCodigo, @Fecha);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleAsync<int>(new CommandDefinition(sql, a, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Adjunto>> GetByTicketAsync(int ticketId, CancellationToken ct = default)
    {
        var sql = $"SELECT {Columns} FROM dbo.Adjuntos WHERE TicketId = @TicketId ORDER BY Fecha;";
        await using var conn = connectionFactory.CreateTicketsConnection();
        return (await conn.QueryAsync<Adjunto>(new CommandDefinition(sql, new { TicketId = ticketId }, cancellationToken: ct))).ToList();
    }

    public async Task<int> CountByTicketAsync(int ticketId, CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.Adjuntos WHERE TicketId = @TicketId;";
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { TicketId = ticketId }, cancellationToken: ct));
    }

    public async Task<Adjunto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var sql = $"SELECT {Columns} FROM dbo.Adjuntos WHERE Id = @Id;";
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleOrDefaultAsync<Adjunto>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM dbo.Adjuntos WHERE Id = @Id;";
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct)) > 0;
    }
}

public sealed class TicketLogRepository(ISqlConnectionFactory connectionFactory) : ITicketLogRepository
{
    public async Task InsertAsync(TicketLogEntry e, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.TicketLog (TicketId, Accion, Descripcion, ValorAnterior, ValorNuevo, UsuarioCodigo, FechaHora)
            VALUES (@TicketId, @Accion, @Descripcion, @ValorAnterior, @ValorNuevo, @UsuarioCodigo, @FechaHora);
            """;
        await using var conn = connectionFactory.CreateTicketsConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, e, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<TicketLogEntry>> GetByTicketAsync(int ticketId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, TicketId, Accion, Descripcion, ValorAnterior, ValorNuevo, UsuarioCodigo, FechaHora
            FROM dbo.TicketLog WHERE TicketId = @TicketId ORDER BY FechaHora DESC, Id DESC;
            """;
        await using var conn = connectionFactory.CreateTicketsConnection();
        return (await conn.QueryAsync<TicketLogEntry>(new CommandDefinition(sql, new { TicketId = ticketId }, cancellationToken: ct))).ToList();
    }
}
