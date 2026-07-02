using Dapper;
using Tickets.Application.Abstractions;
using Tickets.Domain.Entities;
using Tickets.Infrastructure.Persistence;

namespace Tickets.Infrastructure.Repositories;

public sealed class EmpleadoRepository(ISqlConnectionFactory connectionFactory) : IEmpleadoRepository
{
    private const string Columns = "Id, Codigo, Nombre, Correo, Telefono, Activo, FechaAlta";

    public async Task<IReadOnlyList<Empleado>> GetActiveAsync(CancellationToken ct = default)
    {
        var sql = $"SELECT {Columns} FROM dbo.Empleados WHERE Activo = 1 ORDER BY Nombre;";
        await using var conn = connectionFactory.CreateTicketsConnection();
        return (await conn.QueryAsync<Empleado>(new CommandDefinition(sql, cancellationToken: ct))).ToList();
    }

    public async Task<Empleado?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var sql = $"SELECT {Columns} FROM dbo.Empleados WHERE Id = @Id;";
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleOrDefaultAsync<Empleado>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<Empleado?> GetByCodigoAsync(string codigo, CancellationToken ct = default)
    {
        var sql = $"SELECT TOP 1 {Columns} FROM dbo.Empleados WHERE Codigo = @Codigo ORDER BY Id;";
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleOrDefaultAsync<Empleado>(new CommandDefinition(sql, new { Codigo = codigo }, cancellationToken: ct));
    }

    public async Task<int> InsertAsync(Empleado e, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.Empleados (Codigo, Nombre, Correo, Telefono, Activo, FechaAlta)
            VALUES (@Codigo, @Nombre, @Correo, @Telefono, @Activo, @FechaAlta);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleAsync<int>(new CommandDefinition(sql, e, cancellationToken: ct));
    }
}
