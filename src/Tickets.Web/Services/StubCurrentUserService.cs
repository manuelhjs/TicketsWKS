using Tickets.Application.Abstractions;
using Tickets.Domain.Enums;

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
        FullName = section["FullName"] ?? UserCode;
        DepartmentCode = section["DepartmentCode"];
        DepartmentName = section["DepartmentName"] ?? DepartmentCode;
        Position = section["Position"];
        Role = Enum.TryParse<TicketRole>(section["Role"], ignoreCase: true, out var role) ? role : TicketRole.It;
    }

    public string UserCode { get; }
    public string FullName { get; }
    public string? DepartmentCode { get; }
    public string? DepartmentName { get; }
    public string? Position { get; }
    public TicketRole Role { get; }
    public bool IsIt => Role == TicketRole.It;
    public bool CanManageTickets => Role == TicketRole.It;
}
