namespace Tickets.Domain.Entities;

/// <summary>Empleado (solicitante/responsable). Tabla propia del módulo.</summary>
public class Empleado
{
    public int Id { get; set; }
    public string? Codigo { get; set; }        // enlace opcional a usuario SAP
    public string Nombre { get; set; } = string.Empty;
    public string? Correo { get; set; }        // opcional
    public string? Telefono { get; set; }      // opcional
    public string? Puesto { get; set; }
    public string? Area { get; set; }
    public DateOnly? FechaIngreso { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaAlta { get; set; }
}
