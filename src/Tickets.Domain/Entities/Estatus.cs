namespace Tickets.Domain.Entities;

/// <summary>Estatus del ciclo de vida del ticket (catálogo, gestionado por TI).</summary>
public class Estatus
{
    public byte Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public byte Orden { get; set; }
    public bool EsFinal { get; set; }
    public bool Activo { get; set; } = true;
}
