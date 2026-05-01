using Dapper;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Database;
using LinkTracker.Scrapper.Infrastructure.Sql.ModelExtensions;

namespace LinkTracker.Scrapper.Infrastructure.Sql.Repositories;

public class LinkRepository(DatabaseSession dbSession) : ILinkRepository
{
    public async Task<Link> GetOrCreateAsync(Uri uri, LinkSourceType sourceType)
    {
        var dbConnection = await dbSession.OpenConnectionAsync();
        var link = await dbConnection.QuerySingleAsync<Models.Link>(
            """
                INSERT INTO links(url, source_type) VALUES (@url, @sourceType)
                ON CONFLICT(url) DO UPDATE
                SET url = EXCLUDED.url
                RETURNING *
            """, new { url = uri.AbsoluteUri, sourceType = (int)sourceType }, transaction: dbSession.Transaction);

        return link.ToDomain();
    }

    public async Task<bool> RemoveLinkAsync(Uri uri)
    {
        var dbConnection = await dbSession.OpenConnectionAsync();
        var affectedRows = await dbConnection.ExecuteAsync(
            """
                DELETE FROM links WHERE url = @url
            """, new { url = uri.AbsoluteUri }, transaction: dbSession.Transaction);
        return affectedRows > 0;
    }
}
