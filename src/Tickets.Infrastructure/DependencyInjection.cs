using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tickets.Application.Abstractions;
using Tickets.Infrastructure.Persistence;
using Tickets.Infrastructure.Repositories;

namespace Tickets.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        DapperConfig.Register();

        var ticketsConn = configuration.GetConnectionString("TicketsDb")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'TicketsDb'.");
        var directoryConn = configuration.GetConnectionString("SapDirectory"); // opcional

        services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory(ticketsConn, directoryConn));

        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ITicketCommentRepository, TicketCommentRepository>();
        services.AddScoped<ICatalogRepository, CatalogRepository>();

        // Directorio de usuarios: SAP si hay conexión configurada; si no, implementación vacía.
        if (string.IsNullOrWhiteSpace(directoryConn))
        {
            services.AddScoped<IUserDirectoryRepository, NullUserDirectoryRepository>();
        }
        else
        {
            var usersView = configuration["Sap:UsersView"] ?? "dbo.VL_USUARIOS";
            services.AddScoped<IUserDirectoryRepository>(sp =>
                new SapUserDirectoryRepository(sp.GetRequiredService<ISqlConnectionFactory>(), usersView));
        }

        return services;
    }
}
