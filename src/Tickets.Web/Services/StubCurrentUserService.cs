using Tickets.Application.Abstractions;

namespace Tickets.Web.Services;

/// <summary>
/// Implementación temporal de ICurrentUserService (sin autenticación).
/// Toma el código de usuario de la sección "CurrentUser" de appsettings; el nombre,
/// departamento y puesto se resuelven del directorio SAP (VL_Usuarios) en el controlador.
/// Los valores de configuración se usan solo como respaldo.
/// </summary>
public sealed class StubCurrentUserService : ICurrentUserService
{
    public StubCurrentUserService(IConfiguration configuration)
    {
        var section = configuration.GetSection("CurrentUser");
        UserCode = section["UserCode"] ?? "demo";
        FullName = section["FullName"] ?? UserCode;
        DepartmentCode = section["DepartmentCode"];
        DepartmentName = section["DepartmentName"] ?? DepartmentCode;
        Position = section["Position"];
    }

    public string UserCode { get; }
    public string FullName { get; }
    public string? DepartmentCode { get; }
    public string? DepartmentName { get; }
    public string? Position { get; }
}
