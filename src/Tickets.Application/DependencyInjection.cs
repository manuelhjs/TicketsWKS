using Microsoft.Extensions.DependencyInjection;
using Tickets.Application.Abstractions;
using Tickets.Application.Services;

namespace Tickets.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<ITicketCommentService, TicketCommentService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IEmpleadoService, EmpleadoService>();
        services.AddScoped<IAdjuntoService, AdjuntoService>();
        services.AddScoped<IOptionsAdminService, OptionsAdminService>();
        services.AddScoped<IEmpleadoAdminService, EmpleadoAdminService>();
        return services;
    }
}
