using Tickets.Application.Abstractions;
using Tickets.Application.Common;
using Tickets.Application.Dtos;
using Tickets.Domain.Entities;
using Tickets.Domain.Enums;

namespace Tickets.Application.Services;

/// <summary>
/// Lógica de negocio de tickets. Los controladores solo orquestan; aquí viven las
/// reglas (visibilidad, validaciones, transiciones de estatus, autorización).
/// </summary>
public sealed class TicketService(
    ITicketRepository ticketRepository,
    ICatalogRepository catalogRepository,
    IUserDirectoryRepository userDirectory,
    ICurrentUserService currentUser) : ITicketService
{
    public async Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default)
        => await ticketRepository.GetDashboardAsync(BuildVisibility(), ct);

    public async Task<PagedResult<TicketListItemDto>> GetTicketsAsync(TicketFilterDto filter, CancellationToken ct = default)
    {
        var query = BuildVisibility() with
        {
            Status = filter.Status,
            RequesterUserCode = NullIfBlank(filter.RequesterUserCode),
            TicketTypeId = filter.TicketTypeId,
            DepartmentCode = NullIfBlank(filter.DepartmentCode),
            ResponsibleUserCode = NullIfBlank(filter.ResponsibleUserCode),
            CreatedFrom = ResolvePeriod(filter.Period),
            Page = filter.Page < 1 ? 1 : filter.Page,
            PageSize = filter.PageSize is < 1 or > 500 ? 50 : filter.PageSize
        };

        var page = await ticketRepository.QueryAsync(query, ct);
        await EnrichWithUserNamesAsync(page.Items, ct);
        return page;
    }

    public async Task<FilterOptionsDto> GetFilterOptionsAsync(CancellationToken ct = default)
    {
        var visibility = BuildVisibility();

        var requesterCodes = await ticketRepository.GetDistinctRequesterCodesAsync(visibility, ct);
        var departmentCodes = await ticketRepository.GetDistinctDepartmentCodesAsync(visibility, ct);
        var responsibleCodes = await ticketRepository.GetDistinctResponsibleCodesAsync(visibility, ct);
        var types = await catalogRepository.GetActiveTicketTypesAsync(ct);

        var allCodes = requesterCodes.Concat(responsibleCodes).Distinct(StringComparer.OrdinalIgnoreCase);
        var directory = await LoadDirectoryAsync(allCodes, ct);

        return new FilterOptionsDto
        {
            Requesters = requesterCodes
                .Select(c => new SelectOption { Value = c, Text = ResolveName(directory, c) })
                .OrderBy(o => o.Text).ToList(),
            Responsibles = responsibleCodes
                .Select(c => new SelectOption { Value = c, Text = ResolveName(directory, c) })
                .OrderBy(o => o.Text).ToList(),
            Departments = departmentCodes
                .Select(c => new SelectOption { Value = c, Text = ResolveDepartmentName(directory, c) })
                .OrderBy(o => o.Text).ToList(),
            TicketTypes = types
                .Select(t => new SelectOption { Value = t.Id.ToString(), Text = t.Name })
                .OrderBy(o => o.Text).ToList()
        };
    }

    public async Task<int> CreateAsync(CreateTicketRequest request, CancellationToken ct = default)
    {
        if (request.TicketTypeId <= 0)
            throw new ValidationException("Debe seleccionar un tipo de solicitud.");
        if (string.IsNullOrWhiteSpace(request.Description))
            throw new ValidationException("La descripción es obligatoria.");

        var type = await catalogRepository.GetTicketTypeAsync(request.TicketTypeId, ct)
            ?? throw new ValidationException("El tipo de solicitud no existe.");

        // Reglas de campos condicionales por área (antes dispersas entre JS y BD).
        if (string.Equals(type.AreaCode, "CAL", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.QualityDepartment))
                throw new ValidationException("El departamento de calidad es obligatorio para el área de Calidad.");
            if (request.Amount is null)
                throw new ValidationException("El monto es obligatorio para el área de Calidad.");
            if (request.Quantity is null)
                throw new ValidationException("La cantidad es obligatoria para el área de Calidad.");
        }
        else if (string.Equals(type.AreaCode, "PD", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.Machine))
                throw new ValidationException("La máquina es obligatoria para el área de Producción.");
        }

        var ticket = new Ticket
        {
            TicketTypeId = type.Id,
            Status = TicketStatus.Open,
            RequesterUserCode = currentUser.UserCode,
            DepartmentCode = currentUser.DepartmentCode,
            ResponsibleUserCode = type.DefaultResponsibleUserCode,
            Description = request.Description.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "S" : request.Category,
            QualityDepartment = request.QualityDepartment,
            Machine = request.Machine,
            Amount = request.Amount,
            Quantity = request.Quantity,
            RegisteredTime = TimeOnly.FromDateTime(DateTime.Now),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        return await ticketRepository.InsertAsync(ticket, ct);
    }

    public async Task UpdateStatusAsync(UpdateTicketStatusRequest request, CancellationToken ct = default)
    {
        var ticket = await ticketRepository.GetByIdAsync(request.TicketId, ct)
            ?? throw new NotFoundException($"Ticket {request.TicketId} no encontrado.");

        DateTime? closedAt = null;
        TimeOnly? closedTime = null;
        DateOnly? estimatedClose = null;

        switch (request.Status)
        {
            case TicketStatus.Closed:
                closedAt = DateTime.UtcNow;
                closedTime = TimeOnly.FromDateTime(DateTime.Now);
                break;
            case TicketStatus.InProgress:
                if (request.EstimatedCloseDate is null)
                    throw new ValidationException("Debe indicar la fecha estimada de cierre para poner el ticket En Proceso.");
                estimatedClose = request.EstimatedCloseDate;
                break;
        }

        var ok = await ticketRepository.UpdateStatusAsync(
            ticket.Id, request.Status, closedAt, closedTime, estimatedClose, ct);

        if (!ok) throw new ValidationException("No se pudo actualizar el estatus del ticket.");
    }

    public async Task UpdateEstimatedCloseDateAsync(UpdateEstimatedCloseDateRequest request, CancellationToken ct = default)
    {
        await EnsureExistsAsync(request.TicketId, ct);
        var ok = await ticketRepository.UpdateEstimatedCloseDateAsync(request.TicketId, request.EstimatedCloseDate, ct);
        if (!ok) throw new ValidationException("No se pudo actualizar la fecha estimada de cierre.");
    }

    public async Task UpdateResponsibleAsync(UpdateResponsibleRequest request, CancellationToken ct = default)
    {
        if (!currentUser.CanManageTickets)
            throw new ForbiddenException("No tiene permiso para cambiar el responsable.");
        if (string.IsNullOrWhiteSpace(request.ResponsibleUserCode))
            throw new ValidationException("El responsable no es válido.");

        await EnsureExistsAsync(request.TicketId, ct);
        var ok = await ticketRepository.UpdateResponsibleAsync(request.TicketId, request.ResponsibleUserCode.Trim(), ct);
        if (!ok) throw new ValidationException("No se pudo actualizar el responsable.");
    }

    public async Task UpdateCategoryAsync(UpdateCategoryRequest request, CancellationToken ct = default)
    {
        if (!currentUser.CanManageTickets)
            throw new ForbiddenException("No tiene permiso para cambiar el tipo de ticket.");

        await EnsureExistsAsync(request.TicketId, ct);
        var ok = await ticketRepository.UpdateCategoryAsync(request.TicketId, request.Category, ct);
        if (!ok) throw new ValidationException("No se pudo actualizar el tipo de ticket.");
    }

    public async Task SetInactiveAsync(int id, CancellationToken ct = default)
    {
        if (!currentUser.CanManageTickets)
            throw new ForbiddenException("No tiene permiso para inactivar tickets.");

        await EnsureExistsAsync(id, ct);
        var ok = await ticketRepository.SetInactiveAsync(id, ct);
        if (!ok) throw new ValidationException("No se pudo inactivar el ticket.");
    }

    public async Task SetAttachmentAsync(int id, string fileName, CancellationToken ct = default)
    {
        await EnsureExistsAsync(id, ct);
        var ok = await ticketRepository.SetAttachmentAsync(id, fileName, ct);
        if (!ok) throw new ValidationException("No se pudo asociar el archivo adjunto.");
    }

    // ---------- Helpers privados ----------

    private async Task EnsureExistsAsync(int id, CancellationToken ct)
    {
        _ = await ticketRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Ticket {id} no encontrado.");
    }

    private TicketQuery BuildVisibility()
    {
        // Un usuario que puede ver todos: sin restricción.
        // Si no, ve por departamento (si tiene el permiso) o solo los propios.
        var seeAll = currentUser.CanSeeAllTickets;
        return new TicketQuery
        {
            RestrictToRequester = !seeAll && !currentUser.CanSeeDepartmentTickets,
            RestrictToDepartment = !seeAll && currentUser.CanSeeDepartmentTickets,
            CurrentUserCode = currentUser.UserCode,
            CurrentDepartmentCode = currentUser.DepartmentCode
        };
    }

    private static DateTime? ResolvePeriod(string? period) => period switch
    {
        null or "all" => null,
        "lastMonth" => DateTime.Now.AddMonths(-1),
        "last3Months" => DateTime.Now.AddMonths(-3),
        "last6Months" => DateTime.Now.AddMonths(-6),
        "lastYear" => DateTime.Now.AddYears(-1),
        "last2Years" => DateTime.Now.AddYears(-2),
        _ => DateTime.Now.AddYears(-2)
    };

    private async Task EnrichWithUserNamesAsync(IReadOnlyList<TicketListItemDto> items, CancellationToken ct)
    {
        if (items.Count == 0) return;

        var codes = items.SelectMany(i => new[] { i.RequesterUserCode, i.ResponsibleUserCode })
            .Where(c => !string.IsNullOrWhiteSpace(c))!
            .Cast<string>();

        var directory = await LoadDirectoryAsync(codes, ct);

        foreach (var item in items)
        {
            item.RequesterName = ResolveName(directory, item.RequesterUserCode);
            item.ResponsibleName = string.IsNullOrWhiteSpace(item.ResponsibleUserCode)
                ? null : ResolveName(directory, item.ResponsibleUserCode!);

            if (string.IsNullOrWhiteSpace(item.DepartmentName) && !string.IsNullOrWhiteSpace(item.DepartmentCode))
                item.DepartmentName = ResolveDepartmentName(directory, item.DepartmentCode!);
        }
    }

    private async Task<Dictionary<string, DirectoryUser>> LoadDirectoryAsync(IEnumerable<string> codes, CancellationToken ct)
    {
        var distinct = codes.Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (distinct.Count == 0) return new(StringComparer.OrdinalIgnoreCase);

        var users = await userDirectory.GetByCodesAsync(distinct, ct);
        var map = new Dictionary<string, DirectoryUser>(StringComparer.OrdinalIgnoreCase);
        foreach (var u in users) map[u.Code] = u;
        return map;
    }

    private static string ResolveName(IReadOnlyDictionary<string, DirectoryUser> directory, string code)
        => directory.TryGetValue(code, out var u) && !string.IsNullOrWhiteSpace(u.Name) ? u.Name : code;

    private static string ResolveDepartmentName(IReadOnlyDictionary<string, DirectoryUser> directory, string code)
    {
        // Resuelve el nombre del departamento a partir de cualquier usuario de ese departamento.
        var match = directory.Values.FirstOrDefault(u =>
            string.Equals(u.DepartmentCode, code, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(u.DepartmentName));
        return match?.DepartmentName ?? code;
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
