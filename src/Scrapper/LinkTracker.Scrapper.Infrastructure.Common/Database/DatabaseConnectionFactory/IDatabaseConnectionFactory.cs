using System.Data.Common;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.Database.DatabaseConnectionFactory;

public interface IDatabaseConnectionFactory
{
    Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
    DbConnection CreateConnection();
}
