namespace Tickets.Domain.Entities;

/// <summary>Área funcional a la que pertenecen los tipos de solicitud (antes VW_GP_AREAS).</summary>
public class Area
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
