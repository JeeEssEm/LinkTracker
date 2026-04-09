using System.Data;
using System.Data.Common;

namespace LinkTracker.Scrapper.Infrastructure.Sql.DatabaseConnectionFactory;

public interface IDatabaseConnectionFactory
{
    Task<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
