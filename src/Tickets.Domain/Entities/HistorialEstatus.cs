namespace Tickets.Domain.Entities;

/// <summary>Bitácora específica de cambios de estatus (comentario obligatorio).</summary>
public class HistorialEstatus
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public byte? EstatusAnteriorId { get; set; }
    public byte EstatusNuevoId { get; set; }
    public string Comentario { get; set; } = string.Empty;
    public string UsuarioCodigo { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}
