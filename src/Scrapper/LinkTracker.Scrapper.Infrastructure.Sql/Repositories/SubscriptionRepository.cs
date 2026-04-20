using Dapper;
using LinkTracker.Scrapper.Application.ReadModels;
using LinkTracker.Scrapper.Application.Repositories;

namespace LinkTracker.Scrapper.Infrastructure.Sql.Repositories;

public class SubscriptionRepository(DatabaseSession dbSession) : ISubscriptionRepository
{
    public async Task<bool> SubscriptionExistsAsync(long chatId, long linkId)
    {
        var dbConnection = await dbSession.GetConnectionAsync();
        return await dbConnection.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS(
                SELECT 1 FROM subscriptions WHERE chat_id = @chatId AND link_id = @linkId
            )
            """, new { chatId, linkId }, transaction: dbSession.Transaction);
    }

    public async Task<bool> SubscriptionExistsAsync(long chatId, string url)
    {
        var dbConnection = await dbSession.GetConnectionAsync();
        return await dbConnection.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS(
                SELECT 1 FROM subscriptions AS sub
                    INNER JOIN links AS l ON
                        sub.link_id = l.id AND
                        sub.chat_id = @chatId AND
                        l.url = @url
            )
            """, new { chatId, url }, transaction: dbSession.Transaction);
    }

    public async Task CreateSubscriptionAsync(long chatId, long linkId)
    {
        var dbConnection = await dbSession.GetConnectionAsync();
        await dbConnection.ExecuteAsync(
            """
            INSERT INTO subscriptions(chat_id, link_id) VALUES (@chatId, @linkId)
            """, new { chatId, linkId }, transaction: dbSession.Transaction);
    }

    public async Task RemoveSubscriptionAsync(long chatId, string url)
    {
        var dbConnection = await dbSession.GetConnectionAsync();
        await dbConnection.ExecuteAsync(
            """
            DELETE FROM subscriptions AS sub
            USING links AS l
            WHERE sub.chat_id = @chatId AND l.url = @url AND l.id = sub.link_id
            """, new { chatId, url }, transaction: dbSession.Transaction);
    }

    public async Task<SubscribedLink?> GetSubscriptionAsync(long chatId, long linkId)
    {
        var query =
            """
            SELECT l.id,
                   l.url,
                   l.checker_key,
                   t.id AS tag_id,
                   t.name
            FROM links AS l
                INNER JOIN subscriptions AS s ON s.link_id = l.id AND s.chat_id = @chatId
                LEFT JOIN link_tags AS lt ON l.id = lt.link_id
                LEFT JOIN tags AS t ON t.chat_id = @chatId AND t.id = lt.tag_id
            WHERE l.id = @linkId
            """;
        var res = await FetchSubscriptionsAsync(query, new { chatId, linkId }, "tag_id");

        if (res.Count == 0)
        {
            return null;
        }

        return res[0];
    }

    public async Task<SubscribedLink?> GetSubscriptionAsync(long chatId, string url)
    {
        var query =
            """
            SELECT l.id,
                   l.url,
                   l.checker_key,
                   t.id AS tag_id,
                   t.name
            FROM links AS l
                INNER JOIN subscriptions AS s ON s.link_id = l.id AND s.chat_id = @chatId
                LEFT JOIN link_tags AS lt ON l.id = lt.link_id
                LEFT JOIN tags AS t ON t.chat_id = @chatId AND t.id = lt.tag_id
            WHERE l.url = @url
            """;
        var res = await FetchSubscriptionsAsync(query, new { chatId, url }, "tag_id");

        if (res.Count == 0)
        {
            return null;
        }

        return res[0];
    }

    public async Task<List<SubscribedLink>> GetSubscriptionsByChatAsync(long chatId)
    {
        var query =
            """
            SELECT l.id,
                   l.url,
                   l.checker_key,
                   t.id AS tag_id,
                   t.name
            FROM links AS l
                INNER JOIN subscriptions AS s ON s.link_id = l.id AND s.chat_id = @chatId
                LEFT JOIN link_tags AS lt ON l.id = lt.link_id
                LEFT JOIN tags AS t ON t.chat_id = @chatId AND t.id = lt.tag_id
            """;
        return await FetchSubscriptionsAsync(query, new { chatId }, "tag_id");
    }

    public async Task<List<long>> GetChatIdsByLinkId(long linkId, int limit, long previousChatId)
    {
        var dbConnection = await dbSession.GetConnectionAsync();
        var query =
            """
            SELECT chat_id FROM subscriptions WHERE link_id = @linkId AND chat_id > @previousChatId
            ORDER BY chat_id
            LIMIT @limit
            """;
        var res = await dbConnection.QueryAsync<long>(query, new { linkId, previousChatId, limit });

        return res.ToList();
    }

    private async Task<List<SubscribedLink>> FetchSubscriptionsAsync(string query, object param, string splitOn)
    {
        var dbConnection = await dbSession.GetConnectionAsync();
        var links = new Dictionary<long, Tuple<SubscribedLink, List<string>>>();

        await dbConnection.QueryAsync<Models.Link, Models.Tag?, SubscribedLink>(query, (link, tag) =>
        {
            if (!links.TryGetValue(link.Id, out var currentLink))
            {
                currentLink = Tuple.Create(new SubscribedLink(link.Id, link.Url, []), new List<string>());
                links.Add(link.Id, currentLink);
            }
            if (tag != null && !string.IsNullOrEmpty(tag.Name))
            {
                currentLink.Item2.Add(tag.Name);
            }

            return currentLink.Item1;
        }, param, splitOn: splitOn, transaction: dbSession.Transaction);

        return links.Values.Select(e => e.Item1 with { Tags = e.Item2 }).ToList();
    }
}