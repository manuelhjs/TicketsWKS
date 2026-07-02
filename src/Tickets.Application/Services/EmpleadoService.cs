using System.Text.RegularExpressions;
using Tickets.Application.Abstractions;
using Tickets.Application.Common;
using Tickets.Application.Dtos;
using Tickets.Domain.Entities;

namespace Tickets.Application.Services;

public sealed partial class EmpleadoService(
    IEmpleadoRepository empleadoRepository,
    IUserDirectoryRepository userDirectory,
    ICurrentUserService currentUser) : IEmpleadoService
{
    public async Task<IReadOnlyList<EmpleadoDto>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await empleadoRepository.GetActiveAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<EmpleadoDto> CreateAsync(CreateEmpleadoRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            throw new ValidationException("El nombre del solicitante es obligatorio.");
        if (!string.IsNullOrWhiteSpace(request.Correo) && !EmailRegex().IsMatch(request.Correo.Trim()))
            throw new ValidationException("El correo no tiene un formato válido.");
        if (!string.IsNullOrWhiteSpace(request.Telefono) && !TelefonoRegex().IsMatch(request.Telefono.Trim()))
            throw new ValidationException("El teléfono debe tener 10 dígitos.");

        var empleado = new Empleado
        {
            Nombre = request.Nombre.Trim(),
            Correo = Trim(request.Correo),
            Telefono = Trim(request.Telefono),
            Activo = true,
            FechaAlta = DateTime.UtcNow
        };
        empleado.Id = await empleadoRepository.InsertAsync(empleado, ct);
        return Map(empleado);
    }

    public async Task<EmpleadoDto> EnsureCurrentUserAsync(CancellationToken ct = default)
    {
        var existing = await empleadoRepository.GetByCodigoAsync(currentUser.UserCode, ct);
        if (existing is not null) return Map(existing);

        // Auto-alta del usuario actual con datos del directorio SAP (VL_Usuarios).
        var dir = (await userDirectory.GetByCodesAsync([currentUser.UserCode], ct)).FirstOrDefault();
        var empleado = new Empleado
        {
            Codigo = currentUser.UserCode,
            Nombre = dir?.Name is { Length: > 0 } n ? n : currentUser.FullName,
            Correo = dir?.Email,
            Activo = true,
            FechaAlta = DateTime.UtcNow
        };
        empleado.Id = await empleadoRepository.InsertAsync(empleado, ct);
        return Map(empleado);
    }

    private static EmpleadoDto Map(Empleado e) => new()
    {
        Id = e.Id,
        Codigo = e.Codigo,
        Nombre = e.Nombre,
        Correo = e.Correo,
        Telefono = e.Telefono
    };

    private static string? Trim(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^\d{10}$")]
    private static partial Regex TelefonoRegex();
}
