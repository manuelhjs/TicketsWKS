using Serilog;
using Tickets.Application;
using Tickets.Application.Abstractions;
using Tickets.Infrastructure;
using Tickets.Web.Middleware;
using Tickets.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Serilog: structured logging configurado desde appsettings ---
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// --- MVC ---
var mvc = builder.Services.AddControllersWithViews();
// En desarrollo, recompila las vistas Razor al vuelo (los cambios en .cshtml se ven al refrescar).
if (builder.Environment.IsDevelopment())
    mvc.AddRazorRuntimeCompilation();

// --- Capas de la aplicación ---
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// --- Usuario actual (stub, sin autenticación en esta versión) ---
builder.Services.AddScoped<ICurrentUserService, StubCurrentUserService>();

// --- Almacenamiento de adjuntos ---
builder.Services.AddScoped<IFileStorage, FileStorageService>();

var app = builder.Build();

// Logging de peticiones HTTP
app.UseSerilogRequestLogging();

// Manejo de errores centralizado (debe ir lo más externo posible)
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Tickets}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
