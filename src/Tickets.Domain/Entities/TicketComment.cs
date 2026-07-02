namespace Tickets.Domain.Entities;

/// <summary>Comentario asociado a un ticket (antes @GP_CHAT_TICKETS).</summary>
public class TicketComment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string AuthorUserCode { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
