using System.Text.Json;
using Tickets.Application.Common;

namespace Tickets.Web.Middleware;

/// <summary>
/// Manejo de errores centralizado. Traduce excepciones de la aplicación a códigos
/// HTTP y respuestas JSON coherentes, y registra el detalle con Serilog.
/// Reemplaza los try/catch repetidos que ocultaban errores en el legacy.
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var (status, message) = Map(ex);

            if (status >= 500)
                logger.LogError(ex, "Error no controlado en {Path}", context.Request.Path);
            else
                logger.LogWarning("Solicitud inválida en {Path}: {Message}", context.Request.Path, ex.Message);

            if (context.Response.HasStarted)
            {
                logger.LogWarning("La respuesta ya había comenzado; no se puede escribir el error.");
                return;
            }

            if (ExpectsJson(context.Request))
            {
                context.Response.Clear();
                context.Response.StatusCode = status;
                context.Response.ContentType = "application/json";
                var payload = JsonSerializer.Serialize(new { success = false, message });
                await context.Response.WriteAsync(payload);
            }
            else
            {
                context.Response.Redirect("/Home/Error");
            }
        }
    }

    private static (int Status, string Message) Map(Exception ex) => ex switch
    {
        ValidationException => (StatusCodes.Status400BadRequest, ex.Message),
        NotFoundException => (StatusCodes.Status404NotFound, ex.Message),
        ForbiddenException => (StatusCodes.Status403Forbidden, ex.Message),
        _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado. Intente nuevamente.")
    };

    private static bool ExpectsJson(HttpRequest request)
        => request.Headers.XRequestedWith == "XMLHttpRequest"
           || (request.Headers.Accept.Count > 0 && request.Headers.Accept.ToString().Contains("application/json"));
}
