namespace Tickets.Domain.Entities;

/// <summary>Tipo de solicitud (antes @GP_ASIGNACIONTP). Define el responsable por defecto.</summary>
public class TicketType
{
    public int Id { get; set; }
    public int AreaId { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Código de usuario SAP (externo) que atiende este tipo por defecto.</summary>
    public string? DefaultResponsibleUserCode { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    // Datos de navegación (poblados por joins de lectura)
    public string? AreaCode { get; set; }
    public string? AreaName { get; set; }
}
