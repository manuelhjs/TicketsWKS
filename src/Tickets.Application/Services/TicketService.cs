using System.Text.RegularExpressions;
using Tickets.Application.Abstractions;
using Tickets.Application.Common;
using Tickets.Application.Dtos;
using Tickets.Domain.Entities;
using Tickets.Domain.Enums;

namespace Tickets.Application.Services;

/// <summary>
/// Lógica de negocio de tickets: validaciones, creación, edición (con auditoría en
/// TicketLog), cambio de estatus (con HistorialEstatus + comentario obligatorio) y
/// asignación de responsable. Sin roles: acceso completo.
/// </summary>
public sealed partial class TicketService(
    ITicketRepository ticketRepository,
    ICatalogRepository catalogRepository,
    IEmpleadoRepository empleadoRepository,
    IHistorialEstatusRepository historialRepository,
    ITicketLogRepository logRepository,
    ICurrentUserService currentUser) : ITicketService
{
    private const byte EstatusPorAsignar = 1;

    public Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default)
        => ticketRepository.GetDashboardAsync(ct);

    public Task<IReadOnlyList<TicketListItemDto>> GetTicketsAsync(TicketFilterDto filter, CancellationToken ct = default)
        => ticketRepository.GetListAsync(filter, 5000, ct);

    public async Task<byte[]> ExportCsvAsync(TicketFilterDto filter, CancellationToken ct = default)
    {
        var items = await ticketRepository.GetListAsync(filter, 50000, ct);

        var sb = new System.Text.StringBuilder();
        sb.Append('﻿'); // BOM para que Excel reconozca UTF-8
        sb.AppendLine("Id,Solicitante,Tipo,Clasificacion,Categoria,Prioridad,Estatus,Responsable,Creacion,Cierre");
        foreach (var t in items)
        {
            sb.Append(t.Id).Append(',')
              .Append(Csv(t.SolicitanteNombre)).Append(',')
              .Append(Csv(t.TipoSolicitudNombre)).Append(',')
              .Append(Csv(t.ClasificacionNombre)).Append(',')
              .Append(Csv(t.CategoriaNombre)).Append(',')
              .Append(Csv(t.PrioridadNombre)).Append(',')
              .Append(Csv(t.EstatusNombre)).Append(',')
              .Append(Csv(t.ResponsableNombre)).Append(',')
              .Append(t.CreatedAt.ToString("yyyy-MM-dd HH:mm")).Append(',')
              .Append(t.ClosedAt?.ToString("yyyy-MM-dd HH:mm") ?? "")
              .Append('\n');
        }
        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    /// <summary>Escapa un valor para CSV (comillas dobles si contiene coma, comilla o salto de línea).</summary>
    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.IndexOfAny(['"', ',', '\n', '\r']) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }

    public async Task<TicketDetailDto> GetDetailAsync(int id, CancellationToken ct = default)
        => await ticketRepository.GetDetailAsync(id, ct)
           ?? throw new NotFoundException($"Ticket {id} no encontrado.");

    public async Task<int> CreateAsync(CreateTicketRequest request, CancellationToken ct = default)
    {
        var (categoria, _) = await ValidateCoreAsync(
            request.SolicitanteId, request.ClasificacionId, request.CategoriaId,
            request.PrioridadId, request.Descripcion, request.Correo, request.Celular,
            request.TipoSolicitud, ct);

        var ticket = new Ticket
        {
            SolicitanteId = request.SolicitanteId,
            Correo = Trim(request.Correo),
            Celular = Trim(request.Celular),
            TipoSolicitud = request.TipoSolicitud,
            ClasificacionId = request.ClasificacionId,
            CategoriaId = categoria.Id,
            PrioridadId = request.PrioridadId,
            EstatusId = EstatusPorAsignar,
            Descripcion = request.Descripcion.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var id = await ticketRepository.InsertAsync(ticket, ct);

        // Bitácora de estatus inicial + log de creación.
        await historialRepository.InsertAsync(new HistorialEstatus
        {
            TicketId = id,
            EstatusAnteriorId = null,
            EstatusNuevoId = EstatusPorAsignar,
            Comentario = "Ticket creado.",
            UsuarioCodigo = currentUser.UserCode,
            Fecha = DateTime.UtcNow
        }, ct);

        await LogAsync(id, "Creacion", "Alta del ticket", null, $"#{id}", ct);
        return id;
    }

    public async Task UpdateAsync(UpdateTicketRequest request, CancellationToken ct = default)
    {
        var ticket = await ticketRepository.GetByIdAsync(request.TicketId, ct)
            ?? throw new NotFoundException($"Ticket {request.TicketId} no encontrado.");

        var (categoria, _) = await ValidateCoreAsync(
            ticket.SolicitanteId, request.ClasificacionId, request.CategoriaId,
            request.PrioridadId, request.Descripcion, request.Correo, request.Celular,
            request.TipoSolicitud, ct);

        // Detectar cambios para auditar campo por campo.
        var cambios = new List<(string Campo, string? Antes, string? Ahora)>();
        void Track(string campo, string? antes, string? ahora)
        {
            if (!string.Equals(antes ?? "", ahora ?? "", StringComparison.Ordinal))
                cambios.Add((campo, antes, ahora));
        }

        Track("Correo", ticket.Correo, Trim(request.Correo));
        Track("Celular", ticket.Celular, Trim(request.Celular));
        Track("TipoSolicitud", ticket.TipoSolicitud.ToString(), request.TipoSolicitud.ToString());
        Track("ClasificacionId", ticket.ClasificacionId.ToString(), request.ClasificacionId.ToString());
        Track("CategoriaId", ticket.CategoriaId.ToString(), categoria.Id.ToString());
        Track("PrioridadId", ticket.PrioridadId.ToString(), request.PrioridadId.ToString());
        Track("Descripcion", ticket.Descripcion, request.Descripcion.Trim());

        if (cambios.Count == 0) return;

        ticket.Correo = Trim(request.Correo);
        ticket.Celular = Trim(request.Celular);
        ticket.TipoSolicitud = request.TipoSolicitud;
        ticket.ClasificacionId = request.ClasificacionId;
        ticket.CategoriaId = categoria.Id;
        ticket.PrioridadId = request.PrioridadId;
        ticket.Descripcion = request.Descripcion.Trim();

        var ok = await ticketRepository.UpdateFieldsAsync(ticket, ct);
        if (!ok) throw new ValidationException("No se pudo actualizar el ticket.");

        foreach (var c in cambios)
            await LogAsync(ticket.Id, "EdicionCampo", c.Campo, c.Antes, c.Ahora, ct);
    }

    public async Task ChangeStatusAsync(ChangeStatusRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Comentario))
            throw new ValidationException("El comentario es obligatorio al cambiar de estatus.");

        var ticket = await ticketRepository.GetByIdAsync(request.TicketId, ct)
            ?? throw new NotFoundException($"Ticket {request.TicketId} no encontrado.");

        var nuevoEstatus = await catalogRepository.GetEstatusAsync(request.EstatusId, ct)
            ?? throw new ValidationException("El estatus seleccionado no existe.");

        if (ticket.EstatusId == nuevoEstatus.Id)
            throw new ValidationException("El ticket ya se encuentra en ese estatus.");

        var anteriorId = ticket.EstatusId;
        var closedAt = nuevoEstatus.EsFinal ? DateTime.UtcNow : (DateTime?)null;

        var ok = await ticketRepository.UpdateEstatusAsync(ticket.Id, nuevoEstatus.Id, closedAt, ct);
        if (!ok) throw new ValidationException("No se pudo actualizar el estatus.");

        // Solo HistorialEstatus (no se duplica en TicketLog).
        await historialRepository.InsertAsync(new HistorialEstatus
        {
            TicketId = ticket.Id,
            EstatusAnteriorId = anteriorId,
            EstatusNuevoId = nuevoEstatus.Id,
            Comentario = request.Comentario.Trim(),
            UsuarioCodigo = currentUser.UserCode,
            Fecha = DateTime.UtcNow
        }, ct);
    }

    public async Task AssignResponsableAsync(AssignResponsableRequest request, CancellationToken ct = default)
    {
        var ticket = await ticketRepository.GetByIdAsync(request.TicketId, ct)
            ?? throw new NotFoundException($"Ticket {request.TicketId} no encontrado.");

        var nuevo = await empleadoRepository.GetByIdAsync(request.ResponsableEmpleadoId, ct)
            ?? throw new ValidationException("El responsable seleccionado no existe.");

        var anteriorNombre = ticket.ResponsableEmpleadoId is int prevId
            ? (await empleadoRepository.GetByIdAsync(prevId, ct))?.Nombre
            : null;

        var ok = await ticketRepository.UpdateResponsableAsync(ticket.Id, nuevo.Id, ct);
        if (!ok) throw new ValidationException("No se pudo asignar el responsable.");

        await LogAsync(ticket.Id, "AsignaResponsable", "Cambio de responsable", anteriorNombre, nuevo.Nombre, ct);
    }

    public async Task SetInactiveAsync(int id, CancellationToken ct = default)
    {
        _ = await ticketRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Ticket {id} no encontrado.");

        var ok = await ticketRepository.SetInactiveAsync(id, ct);
        if (!ok) throw new ValidationException("No se pudo inactivar el ticket.");

        await LogAsync(id, "Inactivacion", "El ticket fue inactivado", null, null, ct);
    }

    public Task<IReadOnlyList<HistorialEstatusDto>> GetHistorialAsync(int ticketId, CancellationToken ct = default)
        => historialRepository.GetByTicketAsync(ticketId, ct);

    public async Task<IReadOnlyList<TicketLogDto>> GetLogAsync(int ticketId, CancellationToken ct = default)
    {
        var entries = await logRepository.GetByTicketAsync(ticketId, ct);
        return entries.Select(e => new TicketLogDto
        {
            Id = e.Id,
            Accion = e.Accion,
            Descripcion = e.Descripcion,
            ValorAnterior = e.ValorAnterior,
            ValorNuevo = e.ValorNuevo,
            UsuarioCodigo = e.UsuarioCodigo,
            FechaHora = e.FechaHora
        }).ToList();
    }

    // ---------- Helpers ----------

    private async Task<(Categoria Categoria, Empleado Solicitante)> ValidateCoreAsync(
        int solicitanteId, int clasificacionId, int categoriaId, byte prioridadId,
        string descripcion, string? correo, string? celular, TipoSolicitud tipo, CancellationToken ct)
    {
        if (!Enum.IsDefined(tipo))
            throw new ValidationException("Tipo de solicitud no válido.");
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ValidationException("La descripción es obligatoria.");
        if (descripcion.Length > 2000)
            throw new ValidationException("La descripción no puede exceder 2000 caracteres.");

        var solicitante = await empleadoRepository.GetByIdAsync(solicitanteId, ct)
            ?? throw new ValidationException("El solicitante no existe.");

        _ = await catalogRepository.GetClasificacionAsync(clasificacionId, ct)
            ?? throw new ValidationException("La clasificación no existe.");

        var categoria = await catalogRepository.GetCategoriaAsync(categoriaId, ct)
            ?? throw new ValidationException("La categoría no existe.");
        if (categoria.ClasificacionId != clasificacionId)
            throw new ValidationException("La categoría no corresponde a la clasificación seleccionada.");

        _ = await catalogRepository.GetPrioridadAsync(prioridadId, ct)
            ?? throw new ValidationException("La prioridad no existe.");

        if (!string.IsNullOrWhiteSpace(correo) && !EmailRegex().IsMatch(correo.Trim()))
            throw new ValidationException("El correo no tiene un formato válido.");

        if (!string.IsNullOrWhiteSpace(celular) && !CelularRegex().IsMatch(celular.Trim()))
            throw new ValidationException("El celular debe tener 10 dígitos.");

        return (categoria, solicitante);
    }

    private Task LogAsync(int ticketId, string accion, string? descripcion, string? antes, string? ahora, CancellationToken ct)
        => logRepository.InsertAsync(new TicketLogEntry
        {
            TicketId = ticketId,
            Accion = accion,
            Descripcion = descripcion,
            ValorAnterior = antes,
            ValorNuevo = ahora,
            UsuarioCodigo = currentUser.UserCode,
            FechaHora = DateTime.UtcNow
        }, ct);

    private static string? Trim(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^\d{10}$")]
    private static partial Regex CelularRegex();
}
