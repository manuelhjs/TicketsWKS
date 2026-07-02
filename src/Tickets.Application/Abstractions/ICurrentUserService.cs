namespace Tickets.Application.Abstractions;

/// <summary>
/// Abstrae al usuario actual. En esta versión SIN auth se resuelve con un stub
/// configurable + datos del directorio SAP (VL_Usuarios). No hay roles: todos los
/// usuarios tienen acceso completo.
/// </summary>
public interface ICurrentUserService
{
    string UserCode { get; }

    // Datos para la cabecera de la pantalla (nombre real, depto y puesto vienen de VL_Usuarios)
    string FullName { get; }
    string? DepartmentCode { get; }
    string? DepartmentName { get; }
    string? Position { get; }
}
