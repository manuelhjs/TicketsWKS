using Dapper;
using Tickets.Application.Abstractions;
using Tickets.Domain.Entities;
using Tickets.Infrastructure.Persistence;

namespace Tickets.Infrastructure.Repositories;

public sealed class CatalogRepository(ISqlConnectionFactory connectionFactory) : ICatalogRepository
{
    // ---------- Clasificación ----------
    public async Task<IReadOnlyList<Clasificacion>> GetClasificacionesAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT Id, Nombre, Activo, FechaAlta FROM dbo.Clasificacion WHERE Activo = 1 ORDER BY Nombre;";
        await using var conn = connectionFactory.CreateTicketsConnection();
        return (await conn.QueryAsync<Clasificacion>(new CommandDefinition(sql, cancellationToken: ct))).ToList();
    }

    public async Task<Clasificacion?> GetClasificacionAsync(int id, CancellationToken ct = default)
    {
        const string sql = "SELECT Id, Nombre, Activo, FechaAlta FROM dbo.Clasificacion WHERE Id = @Id;";
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleOrDefaultAsync<Clasificacion>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<Clasificacion?> GetClasificacionByNombreAsync(string nombre, CancellationToken ct = default)
    {
        const string sql = "SELECT Id, Nombre, Activo, FechaAlta FROM dbo.Clasificacion WHERE Nombre = @Nombre;";
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleOrDefaultAsync<Clasificacion>(new CommandDefinition(sql, new { Nombre = nombre }, cancellationToken: ct));
    }

    public async Task<int> InsertClasificacionAsync(Clasificacion c, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.Clasificacion (Nombre, Activo, FechaAlta) VALUES (@Nombre, @Activo, @FechaAlta);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleAsync<int>(new CommandDefinition(sql, c, cancellationToken: ct));
    }

    // ---------- Categoría ----------
    public async Task<IReadOnlyList<Categoria>> GetCategoriasByClasificacionAsync(int clasificacionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, ClasificacionId, Nombre, Activo, FechaAlta FROM dbo.Categoria
            WHERE Activo = 1 AND ClasificacionId = @ClasificacionId ORDER BY Nombre;
            """;
        await using var conn = connectionFactory.CreateTicketsConnection();
        return (await conn.QueryAsync<Categoria>(new CommandDefinition(sql, new { ClasificacionId = clasificacionId }, cancellationToken: ct))).ToList();
    }

    public async Task<Categoria?> GetCategoriaAsync(int id, CancellationToken ct = default)
    {
        const string sql = "SELECT Id, ClasificacionId, Nombre, Activo, FechaAlta FROM dbo.Categoria WHERE Id = @Id;";
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleOrDefaultAsync<Categoria>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<Categoria?> GetCategoriaByNombreAsync(int clasificacionId, string nombre, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, ClasificacionId, Nombre, Activo, FechaAlta FROM dbo.Categoria
            WHERE ClasificacionId = @ClasificacionId AND Nombre = @Nombre;
            """;
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleOrDefaultAsync<Categoria>(
            new CommandDefinition(sql, new { ClasificacionId = clasificacionId, Nombre = nombre }, cancellationToken: ct));
    }

    public async Task<int> InsertCategoriaAsync(Categoria c, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.Categoria (ClasificacionId, Nombre, Activo, FechaAlta)
            VALUES (@ClasificacionId, @Nombre, @Activo, @FechaAlta);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleAsync<int>(new CommandDefinition(sql, c, cancellationToken: ct));
    }

    // ---------- Prioridad / Estatus ----------
    public async Task<IReadOnlyList<Prioridad>> GetPrioridadesAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT Id, Nombre, Descripcion, Orden FROM dbo.Prioridad ORDER BY Orden;";
        await using var conn = connectionFactory.CreateTicketsConnection();
        return (await conn.QueryAsync<Prioridad>(new CommandDefinition(sql, cancellationToken: ct))).ToList();
    }

    public async Task<Prioridad?> GetPrioridadAsync(byte id, CancellationToken ct = default)
    {
        const string sql = "SELECT Id, Nombre, Descripcion, Orden FROM dbo.Prioridad WHERE Id = @Id;";
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleOrDefaultAsync<Prioridad>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Estatus>> GetEstatusListAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT Id, Nombre, Orden, EsFinal FROM dbo.Estatus ORDER BY Orden;";
        await using var conn = connectionFactory.CreateTicketsConnection();
        return (await conn.QueryAsync<Estatus>(new CommandDefinition(sql, cancellationToken: ct))).ToList();
    }

    public async Task<Estatus?> GetEstatusAsync(byte id, CancellationToken ct = default)
    {
        const string sql = "SELECT Id, Nombre, Orden, EsFinal FROM dbo.Estatus WHERE Id = @Id;";
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleOrDefaultAsync<Estatus>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }
}
