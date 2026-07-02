using Tickets.Application.Abstractions;
using Tickets.Domain.Entities;

namespace Tickets.Infrastructure.Repositories;

/// <summary>
/// Implementación vacía usada cuando NO se configura la conexión al directorio de SAP.
/// Permite ejecutar el módulo de forma autónoma; los nombres se muestran como el código.
/// </summary>
public sealed class NullUserDirectoryRepository : IUserDirectoryRepository
{
    public Task<IReadOnlyList<DirectoryUser>> GetByCodesAsync(IEnumerable<string> codes, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DirectoryUser>>([]);
}
