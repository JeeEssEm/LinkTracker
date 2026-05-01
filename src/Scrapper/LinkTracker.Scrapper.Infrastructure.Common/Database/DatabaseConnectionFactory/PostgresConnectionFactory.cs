using System.Data.Common;
using Npgsql;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.Database.DatabaseConnectionFactory;

public class PostgresConnectionFactory : IDatabaseConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresConnectionFactory(string connectionString)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        _dataSource = dataSourceBuilder.Build();
    }

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        return await _dataSource.OpenConnectionAsync(cancellationToken);
    }

    public DbConnection CreateConnection()
    {
        return _dataSource.CreateConnection();
    }
}
