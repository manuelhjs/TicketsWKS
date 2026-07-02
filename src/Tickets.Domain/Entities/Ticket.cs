using Tickets.Domain.Enums;

namespace Tickets.Domain.Entities;

/// <summary>
/// Entidad de dominio del ticket (antes @GP_TICKETS). Representa únicamente el
/// estado persistido; sin acceso a datos, presentación ni envío de correos.
/// </summary>
public class Ticket
{
    public int Id { get; set; }
    public int TicketTypeId { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Open;

    // Referencias externas a maestros de SAP (por código)
    public string RequesterUserCode { get; set; } = string.Empty;
    public string? DepartmentCode { get; set; }
    public string? ResponsibleUserCode { get; set; }

    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? AttachmentFileName { get; set; }

    public string? QualityDepartment { get; set; }
    public string? Machine { get; set; }
    public decimal? Amount { get; set; }
    public decimal? Quantity { get; set; }

    public TimeOnly? RegisteredTime { get; set; }
    public TimeOnly? ClosedTime { get; set; }
    public DateOnly? EstimatedCloseDate { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? SourceDatabase { get; set; }
    public bool IsActive { get; set; } = true;
}
