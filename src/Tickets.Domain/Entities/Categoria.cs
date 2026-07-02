namespace Tickets.Domain.Entities;

/// <summary>Categoría del ticket (catálogo editable, dependiente de Clasificación).</summary>
public class Categoria
{
    public int Id { get; set; }
    public int ClasificacionId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime FechaAlta { get; set; }
}
