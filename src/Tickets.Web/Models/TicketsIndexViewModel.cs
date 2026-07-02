using Tickets.Application.Dtos;

namespace Tickets.Web.Models;

public sealed class TicketsIndexViewModel
{
    public DashboardDto Dashboard { get; init; } = new();

    /// <summary>Rol TI (acceso completo). Si es false, es rol Usuario.</summary>
    public bool IsIt { get; init; }

    public string UserCode { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public string Position { get; init; } = string.Empty;
}
