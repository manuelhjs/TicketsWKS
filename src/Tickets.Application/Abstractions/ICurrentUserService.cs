using Tickets.Domain.Enums;

namespace Tickets.Application.Abstractions;

/// <summary>
/// Abstrae al usuario autenticado. En esta versión SIN auth se resuelve con un
/// stub configurable; al integrar autenticación real solo se cambia la implementación.
/// </summary>
public interface ICurrentUserService
{
    string UserCode { get; }

    // Datos para la cabecera de la pantalla
    string FullName { get; }
    string? DepartmentCode { get; }
    string? DepartmentName { get; }
    string? Position { get; }

    TicketRole Role { get; }

    /// <summary>Rol TI: acceso completo.</summary>
    bool IsIt { get; }

    /// <summary>Puede administrar tickets (cambiar estatus, responsable, tipo, inactivar). Solo TI.</summary>
    bool CanManageTickets { get; }
}
