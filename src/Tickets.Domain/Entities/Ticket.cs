using Tickets.Domain.Enums;

namespace Tickets.Domain.Entities;

/// <summary>
/// Entidad de dominio del ticket. Solo el estado persistido; sin acceso a datos ni presentación.
/// </summary>
public class Ticket
{
    public int Id { get; set; }
    public int SolicitanteId { get; set; }
    public string? Correo { get; set; }
    public string? Celular { get; set; }
    public TipoSolicitud TipoSolicitud { get; set; }
    public int ClasificacionId { get; set; }
    public int CategoriaId { get; set; }
    public byte PrioridadId { get; set; }
    public byte EstatusId { get; set; } = 1; // Por asignar
    public int? ResponsableEmpleadoId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
