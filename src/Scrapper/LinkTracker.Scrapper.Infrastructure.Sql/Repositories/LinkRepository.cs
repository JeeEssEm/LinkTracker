using Dapper;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scheduling;
using LinkTracker.Scrapper.Infrastructure.Sql.ModelExtensions;
using Link = LinkTracker.Scrapper.Domain.Links.Entities.Link;
using Tag = LinkTracker.Scrapper.Domain.Tag.Entities.Tag;

namespace LinkTracker.Scrapper.Infrastructure.Sql.Repositories;

public class LinkRepository(DatabaseSession dbSession) : ILinkRepository
{
    public async Task<Link?> GetLinkAsync(string url)
    {
        var dbConnection = await dbSession.GetConnectionAsync();
        var link = await dbConnection.QueryFirstOrDefaultAsync<Models.Link>(
            "SELECT * FROM links WHERE url = @url", new { url }, transaction: dbSession.Transaction
        );

        return link?.ToDomain();
    }

    public async Task<Link> CreateLink(string url, string checkerKey)
    {
        var dbConnection = await dbSession.GetConnectionAsync();
        var link = await dbConnection.QuerySingleAsync<Models.Link>(
            """
                INSERT INTO links(url, checker_key) VALUES(@url, @checkerKey)
                RETURNING *
            """, new { url, checkerKey }, transaction: dbSession.Transaction);

        return link.ToDomain();
    }

    public async Task<Link?> RemoveLink(string url)
    {
        var dbConnection = await dbSession.GetConnectionAsync();
        var link = await dbConnection.QuerySingleAsync<Models.Link?>("DELETE FROM links WHERE url = @url RETURNING id, url, checker_key", new { url }, transaction: dbSession.Transaction);
        return link?.ToDomain();
    }

    public async Task UpdateCheckDatesAt(long[] linkIds)
    {
        var dbConnection = await dbSession.GetConnectionAsync();
        var now = DateTime.UtcNow;

        var query = @"
        UPDATE links 
        SET last_check_at = @now 
        WHERE id = ANY(@linkIds)";

        await dbConnection.ExecuteAsync(query, new { linkIds, now }, transaction: dbSession.Transaction);
    }

    public async Task<List<LinkCheckEntry>> GetLinksToCheck(int page, int limit, DateTime currentTime)
    {
        var dbConnection = await dbSession.GetConnectionAsync();
        var skip = (page - 1) * limit;
        var query =
            """
            WITH paginated_links AS (
                SELECT id, url, checker_key, last_check_at FROM links
                WHERE last_check_at < @currentTime OR last_check_at IS NULL
                ORDER BY last_check_at
                OFFSET @skip
                LIMIT @limit
            )
            SELECT l.*, sub.chat_id AS chat_id FROM paginated_links AS l
                INNER JOIN subscriptions AS sub ON sub.link_id = l.id
            """;

        var result = new Dictionary<long, Tuple<LinkCheckEntry, List<long>>>();
        await dbConnection.QueryAsync<Models.Link, long, LinkCheckEntry>(query, (link, chatId) =>
        {
            if (!result.TryGetValue(link.Id, out var linkEntry))
            {
                linkEntry = Tuple.Create(new LinkCheckEntry(link.Id, link.Url, link.CheckerKey, link.LastCheckAt, []), new List<long>());
                result.Add(link.Id, linkEntry);
            }

            linkEntry.Item2.Add(chatId);

            return linkEntry.Item1;
        }, new { skip, limit, currentTime }, splitOn: "chat_id", transaction: dbSession.Transaction);

        return result.Values.Select(t => t.Item1 with { ChatIdsToNotify = t.Item2 }).ToList();
    }

    public async Task RemoveLinksAsync(IEnumerable<long> linkIds)
    {
        var dbConnection = await dbSession.GetConnectionAsync();
        var query =
            """
            DELETE FROM links
            WHERE id = ANY(@linkIds)
            """;

        await dbConnection.ExecuteAsync(query, new { linkIds = linkIds.ToArray() }, transaction: dbSession.Transaction);
    }
}