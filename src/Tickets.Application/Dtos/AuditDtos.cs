namespace Tickets.Application.Dtos;

public sealed class HistorialEstatusDto
{
    public int Id { get; set; }
    public string? EstatusAnterior { get; set; }
    public string EstatusNuevo { get; set; } = string.Empty;
    public string Comentario { get; set; } = string.Empty;
    public string UsuarioCodigo { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}

public sealed class AdjuntoDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public string? TipoContenido { get; set; }
    public DateTime Fecha { get; set; }
}

public sealed class TicketLogDto
{
    public long Id { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }
    public string? UsuarioCodigo { get; set; }
    public DateTime FechaHora { get; set; }
}
