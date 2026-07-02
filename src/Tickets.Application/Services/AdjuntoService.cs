using Tickets.Application.Abstractions;
using Tickets.Application.Common;
using Tickets.Application.Dtos;
using Tickets.Domain.Entities;

namespace Tickets.Application.Services;

public sealed class AdjuntoService(
    IAdjuntoRepository adjuntoRepository,
    ITicketRepository ticketRepository,
    ITicketLogRepository logRepository,
    ICurrentUserService currentUser) : IAdjuntoService
{
    public async Task<IReadOnlyList<AdjuntoDto>> GetByTicketAsync(int ticketId, CancellationToken ct = default)
    {
        var list = await adjuntoRepository.GetByTicketAsync(ticketId, ct);
        return list.Select(a => new AdjuntoDto
        {
            Id = a.Id,
            TicketId = a.TicketId,
            NombreOriginal = a.NombreOriginal,
            TamanoBytes = a.TamanoBytes,
            TipoContenido = a.TipoContenido,
            Fecha = a.Fecha
        }).ToList();
    }

    public async Task<int> RecordAsync(int ticketId, string nombreOriginal, string nombreAlmacenado,
        string? tipoContenido, long tamanoBytes, CancellationToken ct = default)
    {
        _ = await ticketRepository.GetByIdAsync(ticketId, ct)
            ?? throw new NotFoundException($"Ticket {ticketId} no encontrado.");

        // Validación en backend (no solo en cliente).
        if (!AttachmentRules.IsAllowedExtension(nombreOriginal))
            throw new ValidationException($"Tipo de archivo no permitido. {AttachmentRules.AlternativeMessage}");
        if (tamanoBytes > AttachmentRules.MaxBytesPerFile)
            throw new ValidationException($"El archivo excede el máximo de 10 MB. {AttachmentRules.AlternativeMessage}");

        var count = await adjuntoRepository.CountByTicketAsync(ticketId, ct);
        if (count >= AttachmentRules.MaxFilesPerTicket)
            throw new ValidationException($"El ticket ya alcanzó el máximo de {AttachmentRules.MaxFilesPerTicket} archivos.");

        var adjunto = new Adjunto
        {
            TicketId = ticketId,
            NombreOriginal = nombreOriginal,
            NombreAlmacenado = nombreAlmacenado,
            TipoContenido = tipoContenido,
            TamanoBytes = tamanoBytes,
            UsuarioCodigo = currentUser.UserCode,
            Fecha = DateTime.UtcNow
        };
        var id = await adjuntoRepository.InsertAsync(adjunto, ct);

        await logRepository.InsertAsync(new TicketLogEntry
        {
            TicketId = ticketId,
            Accion = "AdjuntoAgregado",
            Descripcion = "Evidencia adjuntada",
            ValorNuevo = nombreOriginal,
            UsuarioCodigo = currentUser.UserCode,
            FechaHora = DateTime.UtcNow
        }, ct);

        return id;
    }

    public async Task<(string NombreOriginal, string NombreAlmacenado, string? TipoContenido)?> GetForDownloadAsync(int id, CancellationToken ct = default)
    {
        var a = await adjuntoRepository.GetByIdAsync(id, ct);
        return a is null ? null : (a.NombreOriginal, a.NombreAlmacenado, a.TipoContenido);
    }
}
