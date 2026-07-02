using Tickets.Domain.Enums;

namespace Tickets.Application.Dtos;

/// <summary>Filtros del listado (server-side); DataTables pagina/busca en cliente.</summary>
public sealed class TicketFilterDto
{
    public byte? EstatusId { get; set; }
    public int? ClasificacionId { get; set; }
    public byte? PrioridadId { get; set; }
    public int? SolicitanteId { get; set; }
    public byte? TipoSolicitud { get; set; }

    /// <summary>Rango de fechas de creación (inclusivo).</summary>
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
}

/// <summary>Fila del listado (read model).</summary>
public sealed class TicketListItemDto
{
    public int Id { get; set; }
    public string SolicitanteNombre { get; set; } = string.Empty;
    public byte TipoSolicitud { get; set; }
    public string TipoSolicitudNombre { get; set; } = string.Empty;
    public string ClasificacionNombre { get; set; } = string.Empty;
    public string CategoriaNombre { get; set; } = string.Empty;
    public string PrioridadNombre { get; set; } = string.Empty;
    public byte EstatusId { get; set; }
    public string EstatusNombre { get; set; } = string.Empty;
    public string? ResponsableNombre { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}

/// <summary>Detalle escalar del ticket (para el modal).</summary>
public sealed class TicketDetailDto
{
    public int Id { get; set; }
    public int SolicitanteId { get; set; }
    public string SolicitanteNombre { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string? Celular { get; set; }
    public byte TipoSolicitud { get; set; }
    public string TipoSolicitudNombre { get; set; } = string.Empty;
    public int ClasificacionId { get; set; }
    public string ClasificacionNombre { get; set; } = string.Empty;
    public int CategoriaId { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public byte PrioridadId { get; set; }
    public string PrioridadNombre { get; set; } = string.Empty;
    public byte EstatusId { get; set; }
    public string EstatusNombre { get; set; } = string.Empty;
    public int? ResponsableEmpleadoId { get; set; }
    public string? ResponsableNombre { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}

public sealed class CreateTicketRequest
{
    public int SolicitanteId { get; set; }
    public string? Correo { get; set; }
    public string? Celular { get; set; }
    public TipoSolicitud TipoSolicitud { get; set; }
    public int ClasificacionId { get; set; }
    public int CategoriaId { get; set; }
    public byte PrioridadId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}

public sealed class UpdateTicketRequest
{
    public int TicketId { get; set; }
    public string? Correo { get; set; }
    public string? Celular { get; set; }
    public TipoSolicitud TipoSolicitud { get; set; }
    public int ClasificacionId { get; set; }
    public int CategoriaId { get; set; }
    public byte PrioridadId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}

public sealed class ChangeStatusRequest
{
    public int TicketId { get; set; }
    public byte EstatusId { get; set; }
    public string Comentario { get; set; } = string.Empty;
}

public sealed class AssignResponsableRequest
{
    public int TicketId { get; set; }
    public int ResponsableEmpleadoId { get; set; }
}

public sealed class DashboardDto
{
    public int Total { get; set; }
    public int PorAsignar { get; set; }
    public int EnCurso { get; set; }
    public int Finalizados { get; set; }
}
