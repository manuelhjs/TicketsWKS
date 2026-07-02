using Tickets.Application.Abstractions;
using Tickets.Application.Common;
using Tickets.Application.Dtos;

namespace Tickets.Application.Services;

public sealed class CatalogService(ICatalogRepository catalogRepository) : ICatalogService
{
    public async Task<IReadOnlyList<SelectOption>> GetAreasAsync(CancellationToken ct = default)
    {
        var areas = await catalogRepository.GetActiveAreasAsync(ct);
        return areas.Select(a => new SelectOption { Value = a.Code, Text = a.Name }).ToList();
    }

    public async Task<IReadOnlyList<TicketTypeDto>> GetTicketTypesByAreaAsync(string areaCode, CancellationToken ct = default)
    {
        var types = await catalogRepository.GetTicketTypesByAreaAsync(areaCode, ct);
        return types.Select(t => new TicketTypeDto
        {
            Id = t.Id,
            Name = t.Name,
            AreaId = t.AreaId,
            AreaCode = t.AreaCode,
            DefaultResponsibleUserCode = t.DefaultResponsibleUserCode
        }).ToList();
    }
}
