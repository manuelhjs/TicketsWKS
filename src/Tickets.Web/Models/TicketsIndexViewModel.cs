using Tickets.Application.Dtos;

namespace Tickets.Web.Models;

public sealed class TicketsIndexViewModel
{
    public DashboardDto Dashboard { get; init; } = new();

    public string UserCode { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public string Position { get; init; } = string.Empty;

    // Solicitante por defecto (usuario actual auto-provisionado como Empleado)
    public int CurrentEmpleadoId { get; init; }
    public string CurrentEmpleadoNombre { get; init; } = string.Empty;
}
