namespace Tickets.Domain.Entities;

/// <summary>Archivo de evidencia adjunto a un ticket.</summary>
public class Adjunto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public string NombreAlmacenado { get; set; } = string.Empty;
    public string? TipoContenido { get; set; }
    public long TamanoBytes { get; set; }
    public string? UsuarioCodigo { get; set; }
    public DateTime Fecha { get; set; }
}
