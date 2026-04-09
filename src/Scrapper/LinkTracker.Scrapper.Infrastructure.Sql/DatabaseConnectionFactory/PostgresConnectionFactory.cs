using System.Data.Common;
using Npgsql;

namespace LinkTracker.Scrapper.Infrastructure.Sql.DatabaseConnectionFactory;

public class PostgresConnectionFactory(string connectionString) : IDatabaseConnectionFactory
{
    public async Task<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}