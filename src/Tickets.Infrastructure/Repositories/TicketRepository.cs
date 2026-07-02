using System.Text;
using Dapper;
using Tickets.Application.Abstractions;
using Tickets.Application.Dtos;
using Tickets.Domain.Entities;
using Tickets.Infrastructure.Persistence;

namespace Tickets.Infrastructure.Repositories;

public sealed class TicketRepository(ISqlConnectionFactory connectionFactory) : ITicketRepository
{
    private const string TipoCase =
        "(CASE t.TipoSolicitud WHEN 1 THEN N'Incidencia' WHEN 2 THEN N'Requerimiento' ELSE N'' END)";

    private const string FromJoins = """
        FROM dbo.Tickets t
        INNER JOIN dbo.Empleados     solE ON solE.Id = t.SolicitanteId
        INNER JOIN dbo.Clasificacion cl   ON cl.Id  = t.ClasificacionId
        INNER JOIN dbo.Categoria     cat  ON cat.Id = t.CategoriaId
        INNER JOIN dbo.Prioridad     pr   ON pr.Id  = t.PrioridadId
        INNER JOIN dbo.Estatus       es   ON es.Id  = t.EstatusId
        LEFT  JOIN dbo.Empleados     resE ON resE.Id = t.ResponsableEmpleadoId
        """;

    public async Task<IReadOnlyList<TicketListItemDto>> GetListAsync(
        TicketFilterDto filter, DateTime? createdFrom, int maxRows, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        var where = BuildWhere(filter, createdFrom, p);
        p.Add("@Max", maxRows);

        var sql = $"""
            SELECT TOP (@Max)
                t.Id,
                solE.Nombre AS SolicitanteNombre,
                t.TipoSolicitud,
                {TipoCase} AS TipoSolicitudNombre,
                cl.Nombre  AS ClasificacionNombre,
                cat.Nombre AS CategoriaNombre,
                pr.Nombre  AS PrioridadNombre,
                t.EstatusId,
                es.Nombre  AS EstatusNombre,
                resE.Nombre AS ResponsableNombre,
                t.CreatedAt,
                t.ClosedAt
            {FromJoins}
            {where}
            ORDER BY t.CreatedAt DESC;
            """;

        await using var conn = connectionFactory.CreateTicketsConnection();
        var rows = await conn.QueryAsync<TicketListItemDto>(new CommandDefinition(sql, p, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<TicketDetailDto?> GetDetailAsync(int id, CancellationToken ct = default)
    {
        var sql = $"""
            SELECT
                t.Id,
                t.SolicitanteId,
                solE.Nombre AS SolicitanteNombre,
                t.Correo,
                t.Celular,
                t.TipoSolicitud,
                {TipoCase} AS TipoSolicitudNombre,
                t.ClasificacionId,
                cl.Nombre  AS ClasificacionNombre,
                t.CategoriaId,
                cat.Nombre AS CategoriaNombre,
                t.PrioridadId,
                pr.Nombre  AS PrioridadNombre,
                t.EstatusId,
                es.Nombre  AS EstatusNombre,
                t.ResponsableEmpleadoId,
                resE.Nombre AS ResponsableNombre,
                t.Descripcion,
                t.CreatedAt,
                t.ClosedAt
            {FromJoins}
            WHERE t.Id = @Id AND t.IsActive = 1;
            """;

        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleOrDefaultAsync<TicketDetailDto>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<Ticket?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, SolicitanteId, Correo, Celular, TipoSolicitud, ClasificacionId, CategoriaId,
                   PrioridadId, EstatusId, ResponsableEmpleadoId, Descripcion, CreatedAt, ClosedAt, IsActive
            FROM dbo.Tickets WHERE Id = @Id;
            """;
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleOrDefaultAsync<Ticket>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<int> InsertAsync(Ticket t, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.Tickets
                (SolicitanteId, Correo, Celular, TipoSolicitud, ClasificacionId, CategoriaId,
                 PrioridadId, EstatusId, ResponsableEmpleadoId, Descripcion, CreatedAt, IsActive)
            VALUES
                (@SolicitanteId, @Correo, @Celular, @TipoSolicitud, @ClasificacionId, @CategoriaId,
                 @PrioridadId, @EstatusId, @ResponsableEmpleadoId, @Descripcion, @CreatedAt, @IsActive);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        var p = new DynamicParameters();
        p.Add("@SolicitanteId", t.SolicitanteId);
        p.Add("@Correo", t.Correo);
        p.Add("@Celular", t.Celular);
        p.Add("@TipoSolicitud", (byte)t.TipoSolicitud);
        p.Add("@ClasificacionId", t.ClasificacionId);
        p.Add("@CategoriaId", t.CategoriaId);
        p.Add("@PrioridadId", t.PrioridadId);
        p.Add("@EstatusId", t.EstatusId);
        p.Add("@ResponsableEmpleadoId", t.ResponsableEmpleadoId);
        p.Add("@Descripcion", t.Descripcion);
        p.Add("@CreatedAt", t.CreatedAt);
        p.Add("@IsActive", t.IsActive);

        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleAsync<int>(new CommandDefinition(sql, p, cancellationToken: ct));
    }

    public Task<bool> UpdateFieldsAsync(Ticket t, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.Tickets
            SET Correo = @Correo, Celular = @Celular, TipoSolicitud = @TipoSolicitud,
                ClasificacionId = @ClasificacionId, CategoriaId = @CategoriaId,
                PrioridadId = @PrioridadId, Descripcion = @Descripcion
            WHERE Id = @Id;
            """;
        var p = new DynamicParameters();
        p.Add("@Id", t.Id);
        p.Add("@Correo", t.Correo);
        p.Add("@Celular", t.Celular);
        p.Add("@TipoSolicitud", (byte)t.TipoSolicitud);
        p.Add("@ClasificacionId", t.ClasificacionId);
        p.Add("@CategoriaId", t.CategoriaId);
        p.Add("@PrioridadId", t.PrioridadId);
        p.Add("@Descripcion", t.Descripcion);
        return ExecuteAsync(sql, p, ct);
    }

    public Task<bool> UpdateEstatusAsync(int id, byte estatusId, DateTime? closedAt, CancellationToken ct = default)
        => ExecuteAsync(
            "UPDATE dbo.Tickets SET EstatusId = @Estatus, ClosedAt = @ClosedAt WHERE Id = @Id;",
            new DynamicParameters(new { Id = id, Estatus = estatusId, ClosedAt = closedAt }), ct);

    public Task<bool> UpdateResponsableAsync(int id, int responsableEmpleadoId, CancellationToken ct = default)
        => ExecuteAsync(
            "UPDATE dbo.Tickets SET ResponsableEmpleadoId = @Resp WHERE Id = @Id;",
            new DynamicParameters(new { Id = id, Resp = responsableEmpleadoId }), ct);

    public Task<bool> SetInactiveAsync(int id, CancellationToken ct = default)
        => ExecuteAsync(
            "UPDATE dbo.Tickets SET IsActive = 0 WHERE Id = @Id;",
            new DynamicParameters(new { Id = id }), ct);

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                COUNT(1) AS Total,
                SUM(CASE WHEN EstatusId = 1 THEN 1 ELSE 0 END) AS PorAsignar,
                SUM(CASE WHEN EstatusId NOT IN (1, 11, 12) THEN 1 ELSE 0 END) AS EnCurso,
                SUM(CASE WHEN EstatusId = 12 THEN 1 ELSE 0 END) AS Finalizados
            FROM dbo.Tickets WHERE IsActive = 1;
            """;
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleAsync<DashboardDto>(new CommandDefinition(sql, cancellationToken: ct));
    }

    // ---------- Helpers ----------

    private async Task<bool> ExecuteAsync(string sql, DynamicParameters p, CancellationToken ct)
    {
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.ExecuteAsync(new CommandDefinition(sql, p, cancellationToken: ct)) > 0;
    }

    private static string BuildWhere(TicketFilterDto f, DateTime? createdFrom, DynamicParameters p)
    {
        var sb = new StringBuilder("WHERE t.IsActive = 1");
        if (f.EstatusId is not null) { sb.Append(" AND t.EstatusId = @EstatusId"); p.Add("@EstatusId", f.EstatusId.Value); }
        if (f.ClasificacionId is not null) { sb.Append(" AND t.ClasificacionId = @ClasificacionId"); p.Add("@ClasificacionId", f.ClasificacionId.Value); }
        if (f.PrioridadId is not null) { sb.Append(" AND t.PrioridadId = @PrioridadId"); p.Add("@PrioridadId", f.PrioridadId.Value); }
        if (f.SolicitanteId is not null) { sb.Append(" AND t.SolicitanteId = @SolicitanteId"); p.Add("@SolicitanteId", f.SolicitanteId.Value); }
        if (f.TipoSolicitud is not null) { sb.Append(" AND t.TipoSolicitud = @TipoSolicitud"); p.Add("@TipoSolicitud", f.TipoSolicitud.Value); }
        if (createdFrom is not null) { sb.Append(" AND t.CreatedAt >= @CreatedFrom"); p.Add("@CreatedFrom", createdFrom.Value); }
        return sb.ToString();
    }
}
