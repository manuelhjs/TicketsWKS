namespace Tickets.Domain.Enums;

/// <summary>Tipo de solicitud del ticket. Valores fijos (radio en el formulario).</summary>
public enum TipoSolicitud : byte
{
    /// <summary>Algo que está fallando o dejó de funcionar.</summary>
    Incidencia = 1,

    /// <summary>Una solicitud nueva o mejora.</summary>
    Requerimiento = 2
}
