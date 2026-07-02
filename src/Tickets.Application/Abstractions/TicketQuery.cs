using Tickets.Domain.Enums;

namespace Tickets.Application.Abstractions;

/// <summary>
/// Parámetros ya resueltos (incluida la visibilidad por rol/usuario) que el
/// servicio pasa al repositorio. Mantiene las firmas del repositorio pequeñas.
/// </summary>
public sealed record TicketQuery
{
    // Filtros
    public TicketStatus? Status { get; init; }
    public string? RequesterUserCode { get; init; }
    public int? TicketTypeId { get; init; }
    public string? DepartmentCode { get; init; }
    public string? ResponsibleUserCode { get; init; }
    public DateTime? CreatedFrom { get; init; }

    // Visibilidad por rol
    public bool RestrictToRequester { get; init; }
    public string CurrentUserCode { get; init; } = string.Empty;

    /// <summary>Si se define, limita los tickets a estos estatus (rol Usuario: Creado y Cerrado).</summary>
    public IReadOnlyCollection<TicketStatus>? RestrictToStatuses { get; init; }

    /// <summary>Tope de filas devueltas (DataTables pagina en cliente).</summary>
    public int MaxRows { get; init; } = 5000;
}
