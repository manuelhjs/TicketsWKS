using Tickets.Domain.Enums;

namespace Tickets.Application.Abstractions;

/// <summary>
/// Parámetros de consulta que el servicio pasa al repositorio.
/// Mantiene las firmas del repositorio pequeñas.
/// </summary>
public sealed record TicketQuery
{
    public TicketStatus? Status { get; init; }
    public string? RequesterUserCode { get; init; }
    public int? TicketTypeId { get; init; }
    public string? DepartmentCode { get; init; }
    public string? ResponsibleUserCode { get; init; }
    public DateTime? CreatedFrom { get; init; }

    /// <summary>Tope de filas devueltas (DataTables pagina en cliente).</summary>
    public int MaxRows { get; init; } = 5000;
}
