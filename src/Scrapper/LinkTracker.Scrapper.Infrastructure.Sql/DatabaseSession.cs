using System.Data;
using System.Data.Common;
using LinkTracker.Scrapper.Infrastructure.Sql.DatabaseConnectionFactory;

namespace LinkTracker.Scrapper.Infrastructure.Sql;

public class DatabaseSession(IDatabaseConnectionFactory connectionFactory) : IAsyncDisposable
{
    private DbConnection? _dbConnection;

    public DbTransaction? Transaction { get; internal set; }

    public async Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_dbConnection == null)
        {
            _dbConnection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        }
        else if (_dbConnection.State == ConnectionState.Closed)
        {
            await _dbConnection.OpenAsync(cancellationToken);
        }
        else if (_dbConnection.State == ConnectionState.Broken)
        {
            await _dbConnection.DisposeAsync();
            _dbConnection = await connectionFactory.CreateConnectionAsync(cancellationToken);
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