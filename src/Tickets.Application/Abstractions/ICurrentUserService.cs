namespace Tickets.Application.Abstractions;

/// <summary>
/// Abstrae al usuario autenticado. En esta versión SIN auth se resuelve con un
/// stub configurable; al integrar autenticación real solo se cambia la implementación.
/// </summary>
public interface ICurrentUserService
{
    string UserCode { get; }
    string? DepartmentCode { get; }

    /// <summary>Puede ver todos los tickets (rol de seguimiento), no solo los propios.</summary>
    bool CanSeeAllTickets { get; }

    /// <summary>Puede ver los tickets de todo su departamento.</summary>
    bool CanSeeDepartmentTickets { get; }

    /// <summary>Puede administrar tickets (cambiar responsable, inactivar, cambiar tipo).</summary>
    bool CanManageTickets { get; }
}
