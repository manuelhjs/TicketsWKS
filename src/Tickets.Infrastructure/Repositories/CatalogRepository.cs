using Dapper;
using Tickets.Application.Abstractions;
using Tickets.Domain.Entities;
using Tickets.Infrastructure.Persistence;

namespace Tickets.Infrastructure.Repositories;

public sealed class CatalogRepository(ISqlConnectionFactory connectionFactory) : ICatalogRepository
{
    // ================= Clasificación =================
    public async Task<IReadOnlyList<Clasificacion>> GetClasificacionesAsync(CancellationToken ct = default)
        => await QueryClasifAsync("WHERE Activo = 1 ORDER BY Nombre", ct);

    public async Task<IReadOnlyList<Clasificacion>> GetAllClasificacionesAsync(CancellationToken ct = default)
        => await QueryClasifAsync("ORDER BY Nombre", ct);

    private async Task<IReadOnlyList<Clasificacion>> QueryClasifAsync(string tail, CancellationToken ct)
    {
        var sql = $"SELECT Id, Nombre, Activo, FechaAlta FROM dbo.Clasificacion {tail};";
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

    public Task<bool> UpdateClasificacionAsync(Clasificacion c, CancellationToken ct = default)
        => ExecAsync("UPDATE dbo.Clasificacion SET Nombre = @Nombre, Activo = @Activo WHERE Id = @Id;", c, ct);

    public Task<bool> SetClasificacionActivoAsync(int id, bool activo, CancellationToken ct = default)
        => ExecAsync("UPDATE dbo.Clasificacion SET Activo = @Activo WHERE Id = @Id;", new { Id = id, Activo = activo }, ct);

    // ================= Categoría =================
    public async Task<IReadOnlyList<Categoria>> GetCategoriasByClasificacionAsync(int clasificacionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, ClasificacionId, Nombre, Activo, FechaAlta FROM dbo.Categoria
            WHERE Activo = 1 AND ClasificacionId = @ClasificacionId ORDER BY Nombre;
            """;
        await using var conn = connectionFactory.CreateTicketsConnection();
        return (await conn.QueryAsync<Categoria>(new CommandDefinition(sql, new { ClasificacionId = clasificacionId }, cancellationToken: ct))).ToList();
    }

    public async Task<IReadOnlyList<Categoria>> GetAllCategoriasAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT Id, ClasificacionId, Nombre, Activo, FechaAlta FROM dbo.Categoria ORDER BY Nombre;";
        await using var conn = connectionFactory.CreateTicketsConnection();
        return (await conn.QueryAsync<Categoria>(new CommandDefinition(sql, cancellationToken: ct))).ToList();
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
        return await conn.QuerySingleOrDefaultAsync<Categoria>(new CommandDefinition(sql, new { ClasificacionId = clasificacionId, Nombre = nombre }, cancellationToken: ct));
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

    public Task<bool> UpdateCategoriaAsync(Categoria c, CancellationToken ct = default)
        => ExecAsync("UPDATE dbo.Categoria SET ClasificacionId = @ClasificacionId, Nombre = @Nombre, Activo = @Activo WHERE Id = @Id;", c, ct);

    public Task<bool> SetCategoriaActivoAsync(int id, bool activo, CancellationToken ct = default)
        => ExecAsync("UPDATE dbo.Categoria SET Activo = @Activo WHERE Id = @Id;", new { Id = id, Activo = activo }, ct);

    // ================= Prioridad =================
    public async Task<IReadOnlyList<Prioridad>> GetPrioridadesAsync(CancellationToken ct = default)
        => await QueryPrioridadAsync("WHERE Activo = 1 ORDER BY Orden", ct);

    public async Task<IReadOnlyList<Prioridad>> GetAllPrioridadesAsync(CancellationToken ct = default)
        => await QueryPrioridadAsync("ORDER BY Orden", ct);

    private async Task<IReadOnlyList<Prioridad>> QueryPrioridadAsync(string tail, CancellationToken ct)
    {
        var sql = $"SELECT Id, Nombre, Descripcion, Orden, Activo FROM dbo.Prioridad {tail};";
        await using var conn = connectionFactory.CreateTicketsConnection();
        return (await conn.QueryAsync<Prioridad>(new CommandDefinition(sql, cancellationToken: ct))).ToList();
    }

    public async Task<Prioridad?> GetPrioridadAsync(byte id, CancellationToken ct = default)
    {
        const string sql = "SELECT Id, Nombre, Descripcion, Orden, Activo FROM dbo.Prioridad WHERE Id = @Id;";
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleOrDefaultAsync<Prioridad>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<byte> InsertPrioridadAsync(Prioridad p, CancellationToken ct = default)
    {
        const string sql = """
            DECLARE @NewId TINYINT = (SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.Prioridad);
            INSERT INTO dbo.Prioridad (Id, Nombre, Descripcion, Orden, Activo)
            VALUES (@NewId, @Nombre, @Descripcion, @Orden, @Activo);
            SELECT @NewId;
            """;
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleAsync<byte>(new CommandDefinition(sql, p, cancellationToken: ct));
    }

    public Task<bool> UpdatePrioridadAsync(Prioridad p, CancellationToken ct = default)
        => ExecAsync("UPDATE dbo.Prioridad SET Nombre = @Nombre, Descripcion = @Descripcion, Orden = @Orden, Activo = @Activo WHERE Id = @Id;", p, ct);

    public Task<bool> SetPrioridadActivoAsync(byte id, bool activo, CancellationToken ct = default)
        => ExecAsync("UPDATE dbo.Prioridad SET Activo = @Activo WHERE Id = @Id;", new { Id = id, Activo = activo }, ct);

    // ================= Estatus =================
    public async Task<IReadOnlyList<Estatus>> GetEstatusListAsync(CancellationToken ct = default)
        => await QueryEstatusAsync("WHERE Activo = 1 ORDER BY Orden", ct);

    public async Task<IReadOnlyList<Estatus>> GetAllEstatusAsync(CancellationToken ct = default)
        => await QueryEstatusAsync("ORDER BY Orden", ct);

    private async Task<IReadOnlyList<Estatus>> QueryEstatusAsync(string tail, CancellationToken ct)
    {
        var sql = $"SELECT Id, Nombre, Orden, EsFinal, Activo FROM dbo.Estatus {tail};";
        await using var conn = connectionFactory.CreateTicketsConnection();
        return (await conn.QueryAsync<Estatus>(new CommandDefinition(sql, cancellationToken: ct))).ToList();
    }

    public async Task<Estatus?> GetEstatusAsync(byte id, CancellationToken ct = default)
    {
        const string sql = "SELECT Id, Nombre, Orden, EsFinal, Activo FROM dbo.Estatus WHERE Id = @Id;";
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleOrDefaultAsync<Estatus>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<byte> InsertEstatusAsync(Estatus e, CancellationToken ct = default)
    {
        const string sql = """
            DECLARE @NewId TINYINT = (SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.Estatus);
            INSERT INTO dbo.Estatus (Id, Nombre, Orden, EsFinal, Activo)
            VALUES (@NewId, @Nombre, @Orden, @EsFinal, @Activo);
            SELECT @NewId;
            """;
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleAsync<byte>(new CommandDefinition(sql, e, cancellationToken: ct));
    }

    public Task<bool> UpdateEstatusAsync(Estatus e, CancellationToken ct = default)
        => ExecAsync("UPDATE dbo.Estatus SET Nombre = @Nombre, Orden = @Orden, EsFinal = @EsFinal, Activo = @Activo WHERE Id = @Id;", e, ct);

    public Task<bool> SetEstatusActivoAsync(byte id, bool activo, CancellationToken ct = default)
        => ExecAsync("UPDATE dbo.Estatus SET Activo = @Activo WHERE Id = @Id;", new { Id = id, Activo = activo }, ct);

    // ================= Helper =================
    private async Task<bool> ExecAsync(string sql, object param, CancellationToken ct)
    {
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.ExecuteAsync(new CommandDefinition(sql, param, cancellationToken: ct)) > 0;
    }
}
