namespace Tickets.Web.Services;

public interface IFileStorage
{
    Task<string> SaveTicketAttachmentAsync(int ticketId, IFormFile file, CancellationToken ct = default);
    (byte[] Bytes, string FileName)? Read(string fileName);
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

    public async Task<string> SaveTicketAttachmentAsync(int ticketId, IFormFile file, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"Ticket_{ticketId}_{DateTime.Now:dd-MM-yy_HHmmss}{extension}";
        var fullPath = Path.Combine(_root, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream, ct);
        return fileName;
    }

    public (byte[] Bytes, string FileName)? Read(string fileName)
    {
        // Se neutraliza cualquier intento de path traversal.
        var safeName = Path.GetFileName(fileName);
        var fullPath = Path.Combine(_root, safeName);
        if (!File.Exists(fullPath)) return null;
        return (File.ReadAllBytes(fullPath), safeName);
    }
}
