namespace Tickets.Application.Dtos;

public sealed class EmpleadoDto
{
    public int Id { get; set; }
    public string? Codigo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public string? Puesto { get; set; }
    public string? Area { get; set; }
    public DateOnly? FechaIngreso { get; set; }
    public bool Activo { get; set; }
}

/// <summary>Alta rápida desde el modal de tickets (campos mínimos).</summary>
public sealed class CreateEmpleadoRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
}

/// <summary>Alta / edición completa desde el módulo de Empleados (Id = 0 => nuevo).</summary>
public sealed class EmpleadoFormRequest
{
    public int Id { get; set; }
    public string? Codigo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public string? Puesto { get; set; }
    public string? Area { get; set; }
    public DateOnly? FechaIngreso { get; set; }
    public bool Activo { get; set; } = true;
}

public sealed class ImportEmpleadosResultDto
{
    public int Insertados { get; set; }
    public int Actualizados { get; set; }
    public int Omitidos { get; set; }
    public List<string> Errores { get; set; } = [];
}
