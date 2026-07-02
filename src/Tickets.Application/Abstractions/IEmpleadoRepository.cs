using Tickets.Domain.Entities;

namespace Tickets.Application.Abstractions;

public interface IEmpleadoRepository
{
    Task<IReadOnlyList<Empleado>> GetActiveAsync(CancellationToken ct = default);
    Task<Empleado?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Empleado?> GetByCodigoAsync(string codigo, CancellationToken ct = default);
    Task<int> InsertAsync(Empleado empleado, CancellationToken ct = default);
}
