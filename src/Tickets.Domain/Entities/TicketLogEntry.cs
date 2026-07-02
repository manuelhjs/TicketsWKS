namespace Tickets.Domain.Entities;

/// <summary>Registro de auditoría general de un ticket (solo-inserción).</summary>
public class TicketLogEntry
{
    public long Id { get; set; }
    public int TicketId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }
    public string? UsuarioCodigo { get; set; }
    public DateTime FechaHora { get; set; }
}
