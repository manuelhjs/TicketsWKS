using Tickets.Domain.Entities;

namespace Tickets.Application.Abstractions;

/// <summary>
/// Acceso de SOLO LECTURA al directorio de usuarios EXTERNO de SAP (VL_USUARIOS).
/// Este módulo no administra usuarios; solo resuelve nombres/departamentos por código.
/// </summary>
public interface IUserDirectoryRepository
{
    Task<IReadOnlyList<DirectoryUser>> GetByCodesAsync(IEnumerable<string> codes, CancellationToken ct = default);
}
