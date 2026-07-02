using Tickets.Application.Common;

namespace Tickets.Web.Services;

public interface IFileStorage
{
    /// <summary>Valida tipo/tamaño y guarda el archivo. Devuelve el nombre almacenado.</summary>
    Task<(string NombreAlmacenado, long TamanoBytes, string? TipoContenido)> SaveAsync(int ticketId, IFormFile file, CancellationToken ct = default);

    (byte[] Bytes, string FileName)? Read(string nombreAlmacenado);
}

/// <summary>Almacenamiento de adjuntos en el sistema de archivos, con raíz configurable.</summary>
public sealed class FileStorageService : IFileStorage
{
    private readonly string _root;

    public FileStorageService(IConfiguration configuration, IWebHostEnvironment env)
    {
        _root = configuration["Attachments:Path"] is { Length: > 0 } configured
            ? configured
            : Path.Combine(env.ContentRootPath, "attachments");
        Directory.CreateDirectory(_root);
    }

    public async Task<(string NombreAlmacenado, long TamanoBytes, string? TipoContenido)> SaveAsync(
        int ticketId, IFormFile file, CancellationToken ct = default)
    {
        // Validación en backend (además del cliente).
        if (!AttachmentRules.IsAllowedExtension(file.FileName))
            throw new ValidationException($"Tipo de archivo no permitido: {file.FileName}. {AttachmentRules.AlternativeMessage}");
        if (file.Length > AttachmentRules.MaxBytesPerFile)
            throw new ValidationException($"El archivo {file.FileName} excede el máximo de 10 MB. {AttachmentRules.AlternativeMessage}");
        if (file.Length == 0)
            throw new ValidationException($"El archivo {file.FileName} está vacío.");

        var extension = Path.GetExtension(file.FileName);
        var nombreAlmacenado = $"Ticket_{ticketId}_{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_root, nombreAlmacenado);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream, ct);
        return (nombreAlmacenado, file.Length, file.ContentType);
    }

    public (byte[] Bytes, string FileName)? Read(string nombreAlmacenado)
    {
        // Se neutraliza cualquier intento de path traversal.
        var safeName = Path.GetFileName(nombreAlmacenado);
        var fullPath = Path.Combine(_root, safeName);
        if (!File.Exists(fullPath)) return null;
        return (File.ReadAllBytes(fullPath), safeName);
    }
}
