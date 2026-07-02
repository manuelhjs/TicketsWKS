namespace Tickets.Domain.Entities;

/// <summary>Clasificación del ticket (catálogo editable: SAP, CRM, …).</summary>
public class Clasificacion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime FechaAlta { get; set; }
}
