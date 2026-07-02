namespace Tickets.Application.Dtos;

public sealed class PrioridadDto
{
    public byte Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}

public sealed class EstatusDto
{
    public byte Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool EsFinal { get; set; }
}

public sealed class CreateClasificacionRequest
{
    public string Nombre { get; set; } = string.Empty;
}

public sealed class CreateCategoriaRequest
{
    public int ClasificacionId { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
