using Microsoft.Data.SqlClient;

namespace Tickets.Infrastructure.Persistence;

public interface ISqlConnectionFactory
{
    /// <summary>Conexión a la base propia del módulo (TicketsDb).</summary>
    SqlConnection CreateTicketsConnection();

    /// <summary>Conexión al directorio externo de SAP (VL_USUARIOS). Null si no está configurado.</summary>
    SqlConnection? CreateDirectoryConnection();
}

public sealed class SqlConnectionFactory(string ticketsConnectionString, string? directoryConnectionString)
    : ISqlConnectionFactory
{
    public SqlConnection CreateTicketsConnection() => new(ticketsConnectionString);

    public SqlConnection? CreateDirectoryConnection()
        => string.IsNullOrWhiteSpace(directoryConnectionString) ? null : new SqlConnection(directoryConnectionString);
}
