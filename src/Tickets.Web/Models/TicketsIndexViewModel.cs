using Tickets.Application.Dtos;

namespace Tickets.Web.Models;

public sealed class TicketsIndexViewModel
{
    public DashboardDto Dashboard { get; init; } = new();
    public bool CanManage { get; init; }
    public string UserCode { get; init; } = string.Empty;
}
