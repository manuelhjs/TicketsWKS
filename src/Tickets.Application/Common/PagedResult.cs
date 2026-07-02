namespace Tickets.Application.Common;

/// <summary>Resultado paginado genérico para listados.</summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Total { get; init; }
}
