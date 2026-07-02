namespace Tickets.Domain.Enums;

/// <summary>
/// Estatus de un ticket. Los valores coinciden con los Ids del catálogo
/// dbo.TicketStatuses, reemplazando los magic strings 'A'/'EP'/'C' del legacy.
/// </summary>
public enum TicketStatus : byte
{
    Open = 1,        // "Creado"
    InProgress = 2,  // "En Proceso"
    Closed = 3       // "Cerrado"
}
