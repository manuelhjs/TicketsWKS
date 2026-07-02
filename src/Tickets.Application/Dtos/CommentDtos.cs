namespace Tickets.Application.Dtos;

public sealed class TicketCommentDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string AuthorUserCode { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class CreateCommentRequest
{
    public int TicketId { get; set; }
    public string Body { get; set; } = string.Empty;
}
