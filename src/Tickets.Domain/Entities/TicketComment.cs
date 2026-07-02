namespace Tickets.Domain.Entities;

/// <summary>Comentario general asociado a un ticket.</summary>
public class TicketComment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string AutorCodigo { get; set; } = string.Empty;
    public string Comentario { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
