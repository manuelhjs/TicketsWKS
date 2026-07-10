namespace Tickets.Application.Dtos;

// ---------- Lectura (admin: incluye inactivos) ----------
public sealed class ClasificacionAdminDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
}

public sealed class CategoriaAdminDto
{
    public int Id { get; set; }
    public int ClasificacionId { get; set; }
    public string ClasificacionNombre { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
}

public sealed class PrioridadAdminDto
{
    public byte Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public byte Orden { get; set; }
    public bool Activo { get; set; }
}

public sealed class EstatusAdminDto
{
    public byte Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public byte Orden { get; set; }
    public bool EsFinal { get; set; }
    public bool Activo { get; set; }
}

// ---------- Alta / edición (Id = 0 => nuevo) ----------
public sealed class UpsertClasificacionRequest
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}

public sealed class UpsertCategoriaRequest
{
    public int Id { get; set; }
    public int ClasificacionId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}

public sealed class UpsertPrioridadRequest
{
    public byte Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public byte Orden { get; set; }
    public bool Activo { get; set; } = true;
}

public sealed class UpsertEstatusRequest
{
    public byte Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public byte Orden { get; set; }
    public bool EsFinal { get; set; }
    public bool Activo { get; set; } = true;
}
