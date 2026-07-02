using Tickets.Domain.Enums;

namespace Tickets.Application.Abstractions;

/// <summary>
/// Parámetros ya resueltos (incluida la visibilidad por usuario/departamento) que
/// el servicio pasa al repositorio. Mantiene las firmas del repositorio pequeñas.
/// </summary>
public sealed record TicketQuery
{
    public TicketStatus? Status { get; init; }
    public string? RequesterUserCode { get; init; }
    public int? TicketTypeId { get; init; }
    public string? DepartmentCode { get; init; }
    public string? ResponsibleUserCode { get; init; }
    public DateTime? CreatedFrom { get; init; }

    // Visibilidad
    public bool RestrictToRequester { get; init; }
    public bool RestrictToDepartment { get; init; }
    public string CurrentUserCode { get; init; } = string.Empty;
    public string? CurrentDepartmentCode { get; init; }

    // Paginación
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
