namespace Tickets.Domain.Entities;

/// <summary>Prioridad del ticket (catálogo fijo: Alto/Medio/Bajo, con texto SLA).</summary>
public class Prioridad
{
    public byte Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public byte Orden { get; set; }
    public bool Activo { get; set; } = true;
}
