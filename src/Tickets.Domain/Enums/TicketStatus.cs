namespace Tickets.Domain.Enums;

/// <summary>
/// Estatus de un ticket. Los valores coinciden con los Ids del catálogo
/// dbo.TicketStatuses, reemplazando los magic strings 'A'/'EP'/'C' del legacy.
/// </summary>
public enum TicketStatus : byte
{
    Open = 1,
    InProgress = 2,
    Closed = 3
}
