using Tickets.Application.Abstractions;

namespace Tickets.Web.Services;

/// <summary>
/// Implementación temporal de ICurrentUserService (sin autenticación).
/// Toma los valores de la sección "CurrentUser" de appsettings.
/// Al integrar auth real, se sustituye por una que lea el ClaimsPrincipal.
/// </summary>
public sealed class StubCurrentUserService : ICurrentUserService
{
    public StubCurrentUserService(IConfiguration configuration)
    {
        var section = configuration.GetSection("CurrentUser");
        UserCode = section["UserCode"] ?? "demo";
        DepartmentCode = section["DepartmentCode"];
        CanSeeAllTickets = section.GetValue("CanSeeAllTickets", true);
        CanSeeDepartmentTickets = section.GetValue("CanSeeDepartmentTickets", false);
        CanManageTickets = section.GetValue("CanManageTickets", true);
    }

    public string UserCode { get; }
    public string? DepartmentCode { get; }
    public bool CanSeeAllTickets { get; }
    public bool CanSeeDepartmentTickets { get; }
    public bool CanManageTickets { get; }
}
