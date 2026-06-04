using Dapper;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scheduling;
using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Database;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.BulkServices;

public class LinkBulkService(DatabaseSession dbSession) : ILinkBulkRepository
{
    public async Task RemoveLinksAsync(IEnumerable<Guid> linkIds)
    {
        var dbConnection = await dbSession.OpenConnectionAsync();
        var query =
            """
            DELETE FROM links
            WHERE id = ANY(@linkIds)
            """;

        await dbConnection.ExecuteAsync(query, new { linkIds = linkIds.ToArray() }, transaction: dbSession.Transaction);
    }

    public async Task<List<LinkCheckEntry>> GetLinksToCheck(int page, int batchSize, DateTime now)
    {
        var dbConnection = await dbSession.OpenConnectionAsync();
        var skip = (page - 1) * batchSize;
        var query = """
                    WITH paginated_links AS (
                        SELECT id, url, source_type, last_check_at FROM links
                        WHERE last_check_at < @currentTime OR last_check_at IS NULL
                        ORDER BY last_check_at
                        OFFSET @skip
                        LIMIT @limit
                    )
                    SELECT l.*, sub.chat_id AS chat_id FROM paginated_links AS l
                        INNER JOIN subscriptions AS sub ON sub.link_id = l.id
                    """;

        var result = new Dictionary<Guid, Tuple<LinkCheckEntry, List<long>>>();
        await dbConnection.QueryAsync<Models.Link, long, LinkCheckEntry>(query, (link, chatId) =>
        {
            if (!result.TryGetValue(link.Id, out var linkEntry))
            {
                linkEntry = Tuple.Create(new LinkCheckEntry(link.Id, link.Url, (LinkSourceType)link.SourceType, link.LastCheckAt, []), new List<long>());
                result.Add(link.Id, linkEntry);
            }

            linkEntry.Item2.Add(chatId);

            return linkEntry.Item1;
        }, new { skip, limit = batchSize, currentTime = now }, splitOn: "chat_id", transaction: dbSession.Transaction);

        return result.Values.Select(t => t.Item1 with { ChatIdsToNotify = t.Item2 }).ToList();

    }

    public async Task UpdateCheckDatesAt(IEnumerable<Guid> linkIds)
    {
        var dbConnection = await dbSession.OpenConnectionAsync();
        var now = DateTime.UtcNow;

        var query = @"
        UPDATE links 
        SET last_check_at = @now 
        WHERE id = ANY(@linkIds)";

        await dbConnection.ExecuteAsync(query, new { linkIds, now }, transaction: dbSession.Transaction);
    }

    public async Task<Dictionary<LinkSourceType, long>> GetNumberOfTrackedLinks()
    {
        var dbConnection = await dbSession.OpenConnectionAsync();
        var query = @"
            SELECT source_type, COUNT(*) FROM links
            GROUP BY source_type
            ";
        var counts = await dbConnection.QueryAsync<(LinkSourceType SourceType, long Count)>(query);

        return counts.ToDictionary(row => row.SourceType, row => row.Count);
    }
}