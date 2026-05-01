using System.Data;
using System.Data.Common;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Database.DatabaseConnectionFactory;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.Database;

public class DatabaseSession(IDatabaseConnectionFactory connectionFactory) : IAsyncDisposable
{
    private DbConnection? _dbConnection;

    public DbTransaction? Transaction { get; internal set; }

    public DbConnection GetOrCreateConnection()
        => _dbConnection ??= connectionFactory.CreateConnection();

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_dbConnection == null)
        {
            _dbConnection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        }
        else if (_dbConnection.State == ConnectionState.Closed)
        {
            await _dbConnection.OpenAsync(cancellationToken);
        }
        else if (_dbConnection.State == ConnectionState.Broken)
        {
            await _dbConnection.DisposeAsync();
            _dbConnection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        }

        return _dbConnection;
    }

    public async ValueTask DisposeAsync()
    {
        if (Transaction != null)
        {
            await Transaction.DisposeAsync();
            Transaction = null;
        }

        if (_dbConnection != null)
        {
            await _dbConnection.DisposeAsync();
            _dbConnection = null;
        }
    }
}