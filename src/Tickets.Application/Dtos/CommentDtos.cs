namespace Tickets.Application.Dtos;

public sealed class TicketCommentDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string AutorCodigo { get; set; } = string.Empty;
    public string AutorNombre { get; set; } = string.Empty;
    public string Comentario { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class CreateCommentRequest
{
    public int TicketId { get; set; }
    public string Comentario { get; set; } = string.Empty;
}
