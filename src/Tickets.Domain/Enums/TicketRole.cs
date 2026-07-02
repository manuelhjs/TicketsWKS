namespace Tickets.Domain.Enums;

/// <summary>
/// Rol del usuario en el módulo de tickets.
/// - It:   acceso completo (ver/crear/editar todos los tickets).
/// - User: solo ve los tickets que él creó y únicamente en estado Creado o Cerrado.
/// </summary>
public enum TicketRole : byte
{
    It = 0,
    User = 1
}
