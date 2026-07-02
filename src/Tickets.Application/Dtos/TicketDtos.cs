using Tickets.Application.Common;
using Tickets.Domain.Enums;

namespace Tickets.Application.Dtos;

/// <summary>Filtros y paginación del listado de tickets (entrada desde el grid).</summary>
public sealed class TicketFilterDto
{
    public TicketStatus? Status { get; set; }
    public string? RequesterUserCode { get; set; }
    public int? TicketTypeId { get; set; }
    public string? DepartmentCode { get; set; }
    public string? ResponsibleUserCode { get; set; }

    /// <summary>all | lastMonth | last3Months | last6Months | lastYear | last2Years</summary>
    public string? Period { get; set; } = "last2Years";

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>Fila del listado. Read model: NO es la entidad de BD.</summary>
public sealed class TicketListItemDto
{
    public int Id { get; set; }
    public int TicketTypeId { get; set; }
    public string TicketTypeName { get; set; } = string.Empty;
    public string AreaName { get; set; } = string.Empty;

    public string RequesterUserCode { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string? DepartmentCode { get; set; }
    public string? DepartmentName { get; set; }
    public string? ResponsibleUserCode { get; set; }
    public string? ResponsibleName { get; set; }

    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? AttachmentFileName { get; set; }

    public TicketStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;

    public string? QualityDepartment { get; set; }
    public string? Machine { get; set; }
    public decimal? Amount { get; set; }
    public decimal? Quantity { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateOnly? EstimatedCloseDate { get; set; }
    public DateTime? ClosedAt { get; set; }
    public TimeOnly? RegisteredTime { get; set; }
    public TimeOnly? ClosedTime { get; set; }
}

public sealed class CreateTicketRequest
{
    public int TicketTypeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? QualityDepartment { get; set; }
    public string? Machine { get; set; }
    public decimal? Amount { get; set; }
    public decimal? Quantity { get; set; }
}

public sealed class UpdateTicketStatusRequest
{
    public int TicketId { get; set; }
    public TicketStatus Status { get; set; }
    public DateOnly? EstimatedCloseDate { get; set; }
}

public sealed class UpdateEstimatedCloseDateRequest
{
    public int TicketId { get; set; }
    public DateOnly EstimatedCloseDate { get; set; }
}

public sealed class UpdateResponsibleRequest
{
    public int TicketId { get; set; }
    public string ResponsibleUserCode { get; set; } = string.Empty;
}

public sealed class UpdateCategoryRequest
{
    public int TicketId { get; set; }
    public string Category { get; set; } = string.Empty;
}

public sealed class DashboardDto
{
    public int TotalOpen { get; set; }
    public int TotalInProgress { get; set; }
    public int TotalClosed { get; set; }
}

public sealed class FilterOptionsDto
{
    public IReadOnlyList<SelectOption> Requesters { get; set; } = [];
    public IReadOnlyList<SelectOption> TicketTypes { get; set; } = [];
    public IReadOnlyList<SelectOption> Departments { get; set; } = [];
    public IReadOnlyList<SelectOption> Responsibles { get; set; } = [];
}
