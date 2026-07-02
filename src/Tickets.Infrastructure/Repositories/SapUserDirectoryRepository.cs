using Dapper;
using Tickets.Application.Abstractions;
using Tickets.Domain.Entities;
using Tickets.Infrastructure.Persistence;

namespace Tickets.Infrastructure.Repositories;

/// <summary>
/// Lee usuarios del directorio EXTERNO de SAP (VL_USUARIOS) en modo solo lectura.
/// El nombre de la vista es configurable (Sap:UsersView). Si no hay conexión de
/// directorio configurada, devuelve vacío y los nombres caen al código.
/// </summary>
public sealed class SapUserDirectoryRepository(ISqlConnectionFactory connectionFactory, string usersView)
    : IUserDirectoryRepository
{
    public async Task<IReadOnlyList<DirectoryUser>> GetByCodesAsync(IEnumerable<string> codes, CancellationToken ct = default)
    {
        var list = codes.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (list.Length == 0) return [];

        await using var conn = connectionFactory.CreateDirectoryConnection();
        if (conn is null) return [];

        // El nombre de la vista viene de configuración (no de entrada de usuario) -> interpolación segura.
        // Columnas confirmadas en VL_USUARIOS (SAP). Se omite Email: el legacy no lo
        // expone en esta vista y las notificaciones están fuera de alcance.
        var sql = $"""
            SELECT Code, Nombre AS Name, DepCode AS DepartmentCode, DepName AS DepartmentName
            FROM {usersView}
            WHERE Code IN @Codes;
            """;

        var rows = await conn.QueryAsync<DirectoryUser>(new CommandDefinition(sql, new { Codes = list }, cancellationToken: ct));
        return rows.ToList();
    }
}
