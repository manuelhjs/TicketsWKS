namespace Tickets.Application.Dtos;

/// <summary>Tipo de solicitud expuesto al front-end (para el modal de alta).</summary>
public sealed class TicketTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AreaId { get; set; }
    public string? AreaCode { get; set; }
    public string? DefaultResponsibleUserCode { get; set; }
}
