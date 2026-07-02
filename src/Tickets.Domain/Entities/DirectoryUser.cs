namespace Tickets.Domain.Entities;

/// <summary>
/// Usuario proveniente del directorio EXTERNO de SAP (tabla VL_USUARIOS).
/// Es de solo lectura para este módulo; no se administra aquí.
/// </summary>
public class DirectoryUser
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DepartmentCode { get; set; }
    public string? DepartmentName { get; set; }
}
