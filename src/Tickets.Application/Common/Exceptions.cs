namespace Tickets.Application.Common;

/// <summary>Se lanza cuando una entidad solicitada no existe. El middleware la traduce a 404.</summary>
public sealed class NotFoundException(string message) : Exception(message);

/// <summary>Se lanza ante datos de entrada o reglas de negocio inválidas. Se traduce a 400.</summary>
public sealed class ValidationException(string message) : Exception(message);

/// <summary>Se lanza cuando el usuario actual no tiene permiso para la operación. Se traduce a 403.</summary>
public sealed class ForbiddenException(string message) : Exception(message);
