namespace Tickets.Application.Common;

/// <summary>Reglas de adjuntos, validadas también en backend.</summary>
public static class AttachmentRules
{
    public const long MaxBytesPerFile = 10L * 1024 * 1024; // 10 MB
    public const int MaxFilesPerTicket = 5;

    public static readonly IReadOnlySet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".ppt", ".pptx",
        ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv"
    };

    public const string AlternativeMessage =
        "Si no puedes adjuntar el archivo (por tamaño o tipo), comparte un enlace de Google Drive " +
        "o envíalo a plataformas.soporte@impulsoraint.com.";

    public static bool IsAllowedExtension(string fileName)
        => AllowedExtensions.Contains(Path.GetExtension(fileName));
}
