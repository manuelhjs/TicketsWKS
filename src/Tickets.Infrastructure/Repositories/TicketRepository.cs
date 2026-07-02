using System.Text;
using Dapper;
using Tickets.Application.Abstractions;
using Tickets.Application.Dtos;
using Tickets.Domain.Entities;
using Tickets.Domain.Enums;
using Tickets.Infrastructure.Persistence;

namespace Tickets.Infrastructure.Repositories;

public sealed class TicketRepository(ISqlConnectionFactory connectionFactory) : ITicketRepository
{
    private const string BaseColumns = """
        t.Id,
        t.TicketTypeId,
        tt.Name              AS TicketTypeName,
        a.Name               AS AreaName,
        t.RequesterUserCode,
        t.DepartmentCode,
        t.ResponsibleUserCode,
        t.Description,
        t.Category,
        t.AttachmentFileName,
        t.StatusId           AS Status,
        s.Name               AS StatusName,
        t.QualityDepartment,
        t.Machine,
        t.Amount,
        t.Quantity,
        t.CreatedAt,
        t.EstimatedCloseDate,
        t.ClosedAt,
        t.RegisteredTime,
        t.ClosedTime
        """;

    private const string FromJoins = """
        FROM dbo.Tickets t
        INNER JOIN dbo.TicketTypes    tt ON tt.Id = t.TicketTypeId
        INNER JOIN dbo.Areas          a  ON a.Id  = tt.AreaId
        INNER JOIN dbo.TicketStatuses s  ON s.Id  = t.StatusId
        """;

    public async Task<IReadOnlyList<TicketListItemDto>> GetListAsync(TicketQuery query, CancellationToken ct = default)
    {
        var parameters = new DynamicParameters();
        var where = BuildWhere(query, parameters, includeFilters: true);
        parameters.Add("@Max", query.MaxRows);

        var sql = $"""
            SELECT TOP (@Max) {BaseColumns}
            {FromJoins}
            {where}
            ORDER BY t.CreatedAt DESC;
            """;

        await using var conn = connectionFactory.CreateTicketsConnection();
        var rows = await conn.QueryAsync<TicketListItemDto>(new CommandDefinition(sql, parameters, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<TicketListItemDto?> GetListItemByIdAsync(int id, CancellationToken ct = default)
    {
        var sql = $"""
            SELECT {BaseColumns}
            {FromJoins}
            WHERE t.Id = @Id AND t.IsActive = 1;
            """;

        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleOrDefaultAsync<TicketListItemDto>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<DashboardDto> GetDashboardAsync(TicketQuery query, CancellationToken ct = default)
    {
        var parameters = new DynamicParameters();
        // Para el dashboard ignoramos filtros de estatus (contamos por cada estatus).
        var where = BuildWhere(query with { Status = null }, parameters, includeFilters: false);

        var sql = $"""
            SELECT
                SUM(CASE WHEN t.StatusId = @Open       THEN 1 ELSE 0 END) AS TotalOpen,
                SUM(CASE WHEN t.StatusId = @InProgress THEN 1 ELSE 0 END) AS TotalInProgress,
                SUM(CASE WHEN t.StatusId = @Closed     THEN 1 ELSE 0 END) AS TotalClosed
            {FromJoins}
            {where};
            """;

        parameters.Add("@Open", (byte)TicketStatus.Open);
        parameters.Add("@InProgress", (byte)TicketStatus.InProgress);
        parameters.Add("@Closed", (byte)TicketStatus.Closed);

        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleAsync<DashboardDto>(new CommandDefinition(sql, parameters, cancellationToken: ct));
    }

    public async Task<Ticket?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, TicketTypeId, StatusId AS Status, RequesterUserCode, DepartmentCode,
                   ResponsibleUserCode, Description, Category, AttachmentFileName, QualityDepartment,
                   Machine, Amount, Quantity, RegisteredTime, ClosedTime, EstimatedCloseDate,
                   CreatedAt, ClosedAt, SourceDatabase, IsActive
            FROM dbo.Tickets WHERE Id = @Id;
            """;
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleOrDefaultAsync<Ticket>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<int> InsertAsync(Ticket ticket, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.Tickets
                (TicketTypeId, StatusId, RequesterUserCode, DepartmentCode, ResponsibleUserCode,
                 Description, Category, AttachmentFileName, QualityDepartment, Machine, Amount, Quantity,
                 RegisteredTime, EstimatedCloseDate, CreatedAt, SourceDatabase, IsActive)
            VALUES
                (@TicketTypeId, @StatusId, @RequesterUserCode, @DepartmentCode, @ResponsibleUserCode,
                 @Description, @Category, @AttachmentFileName, @QualityDepartment, @Machine, @Amount, @Quantity,
                 @RegisteredTime, @EstimatedCloseDate, @CreatedAt, @SourceDatabase, @IsActive);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        var parameters = new DynamicParameters();
        parameters.Add("@TicketTypeId", ticket.TicketTypeId);
        parameters.Add("@StatusId", (byte)ticket.Status);
        parameters.Add("@RequesterUserCode", ticket.RequesterUserCode);
        parameters.Add("@DepartmentCode", ticket.DepartmentCode);
        parameters.Add("@ResponsibleUserCode", ticket.ResponsibleUserCode);
        parameters.Add("@Description", ticket.Description);
        parameters.Add("@Category", ticket.Category);
        parameters.Add("@AttachmentFileName", ticket.AttachmentFileName);
        parameters.Add("@QualityDepartment", ticket.QualityDepartment);
        parameters.Add("@Machine", ticket.Machine);
        parameters.Add("@Amount", ticket.Amount);
        parameters.Add("@Quantity", ticket.Quantity);
        parameters.Add("@RegisteredTime", ticket.RegisteredTime);
        parameters.Add("@EstimatedCloseDate", ticket.EstimatedCloseDate);
        parameters.Add("@CreatedAt", ticket.CreatedAt);
        parameters.Add("@SourceDatabase", ticket.SourceDatabase);
        parameters.Add("@IsActive", ticket.IsActive);

        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.QuerySingleAsync<int>(new CommandDefinition(sql, parameters, cancellationToken: ct));
    }

    public async Task<bool> UpdateStatusAsync(
        int id, TicketStatus status, DateTime? closedAt, TimeOnly? closedTime,
        DateOnly? estimatedCloseDate, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.Tickets
            SET StatusId           = @StatusId,
                ClosedAt           = @ClosedAt,
                ClosedTime         = @ClosedTime,
                EstimatedCloseDate = COALESCE(@EstimatedCloseDate, EstimatedCloseDate)
            WHERE Id = @Id;
            """;

        var parameters = new DynamicParameters();
        parameters.Add("@Id", id);
        parameters.Add("@StatusId", (byte)status);
        parameters.Add("@ClosedAt", closedAt);
        parameters.Add("@ClosedTime", closedTime);
        parameters.Add("@EstimatedCloseDate", estimatedCloseDate);

        return await ExecuteAsync(sql, parameters, ct);
    }

    public Task<bool> UpdateEstimatedCloseDateAsync(int id, DateOnly date, CancellationToken ct = default)
        => ExecuteAsync(
            "UPDATE dbo.Tickets SET EstimatedCloseDate = @Date WHERE Id = @Id;",
            new DynamicParameters(new { Id = id, Date = date }), ct);

    public Task<bool> UpdateResponsibleAsync(int id, string responsibleUserCode, CancellationToken ct = default)
        => ExecuteAsync(
            "UPDATE dbo.Tickets SET ResponsibleUserCode = @Responsible WHERE Id = @Id;",
            new DynamicParameters(new { Id = id, Responsible = responsibleUserCode }), ct);

    public Task<bool> UpdateCategoryAsync(int id, string category, CancellationToken ct = default)
        => ExecuteAsync(
            "UPDATE dbo.Tickets SET Category = @Category WHERE Id = @Id;",
            new DynamicParameters(new { Id = id, Category = category }), ct);

    public Task<bool> SetInactiveAsync(int id, CancellationToken ct = default)
        => ExecuteAsync(
            "UPDATE dbo.Tickets SET IsActive = 0 WHERE Id = @Id;",
            new DynamicParameters(new { Id = id }), ct);

    public Task<bool> SetAttachmentAsync(int id, string fileName, CancellationToken ct = default)
        => ExecuteAsync(
            "UPDATE dbo.Tickets SET AttachmentFileName = @FileName WHERE Id = @Id;",
            new DynamicParameters(new { Id = id, FileName = fileName }), ct);

    public Task<IReadOnlyList<string>> GetDistinctRequesterCodesAsync(TicketQuery visibility, CancellationToken ct = default)
        => GetDistinctCodesAsync("t.RequesterUserCode", visibility, ct);

    public Task<IReadOnlyList<string>> GetDistinctDepartmentCodesAsync(TicketQuery visibility, CancellationToken ct = default)
        => GetDistinctCodesAsync("t.DepartmentCode", visibility, ct);

    public Task<IReadOnlyList<string>> GetDistinctResponsibleCodesAsync(TicketQuery visibility, CancellationToken ct = default)
        => GetDistinctCodesAsync("t.ResponsibleUserCode", visibility, ct);

    // ---------- Helpers privados ----------

    private async Task<IReadOnlyList<string>> GetDistinctCodesAsync(string column, TicketQuery visibility, CancellationToken ct)
    {
        var parameters = new DynamicParameters();
        var where = BuildWhere(visibility with { Status = null }, parameters, includeFilters: false);
        var sql = $"SELECT DISTINCT {column} {FromJoins} {where} AND {column} IS NOT NULL AND {column} <> '';";

        await using var conn = connectionFactory.CreateTicketsConnection();
        var rows = await conn.QueryAsync<string>(new CommandDefinition(sql, parameters, cancellationToken: ct));
        return rows.ToList();
    }

    private async Task<bool> ExecuteAsync(string sql, DynamicParameters parameters, CancellationToken ct)
    {
        await using var conn = connectionFactory.CreateTicketsConnection();
        return await conn.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: ct)) > 0;
    }

    /// <summary>Construye el WHERE (visibilidad + filtros) de forma 100% parametrizada.</summary>
    private static string BuildWhere(TicketQuery q, DynamicParameters parameters, bool includeFilters)
    {
        var sb = new StringBuilder("WHERE t.IsActive = 1");

        if (!includeFilters) return sb.ToString();

        if (q.Status is not null)
        {
            sb.Append(" AND t.StatusId = @FilterStatus");
            parameters.Add("@FilterStatus", (byte)q.Status.Value);
        }
        if (!string.IsNullOrWhiteSpace(q.RequesterUserCode))
        {
            sb.Append(" AND t.RequesterUserCode = @FilterRequester");
            parameters.Add("@FilterRequester", q.RequesterUserCode);
        }
        if (q.TicketTypeId is not null)
        {
            sb.Append(" AND t.TicketTypeId = @FilterTypeId");
            parameters.Add("@FilterTypeId", q.TicketTypeId.Value);
        }
        if (!string.IsNullOrWhiteSpace(q.DepartmentCode))
        {
            sb.Append(" AND t.DepartmentCode = @FilterDepartment");
            parameters.Add("@FilterDepartment", q.DepartmentCode);
        }
        if (!string.IsNullOrWhiteSpace(q.ResponsibleUserCode))
        {
            sb.Append(" AND t.ResponsibleUserCode = @FilterResponsible");
            parameters.Add("@FilterResponsible", q.ResponsibleUserCode);
        }
        if (q.CreatedFrom is not null)
        {
            sb.Append(" AND t.CreatedAt >= @FilterCreatedFrom");
            parameters.Add("@FilterCreatedFrom", q.CreatedFrom.Value);
        }

        return sb.ToString();
    }
}
