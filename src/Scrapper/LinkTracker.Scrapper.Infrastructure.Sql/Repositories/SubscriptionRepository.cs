using Dapper;
using LinkTracker.Scrapper.Application.ReadModels;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Database;

namespace LinkTracker.Scrapper.Infrastructure.Sql.Repositories;

public class SubscriptionRepository(DatabaseSession dbSession) : ISubscriptionRepository
{
    public async Task<bool> CreateAsync(Subscription subscription)
    {
        var dbConnection = await dbSession.OpenConnectionAsync();
        var query = """
                    WITH inserted_sub AS (
                        INSERT INTO subscriptions(chat_id, link_id) 
                        VALUES (@chatId, @linkId) 
                        ON CONFLICT DO NOTHING
                        RETURNING 1
                    ),
                    ins_tags AS (
                        INSERT INTO tags(name, chat_id, link_id)
                        SELECT name, @chatId, @linkId 
                        FROM UNNEST(@tagNames) AS name
                        WHERE EXISTS (SELECT 1 FROM inserted_sub)
                    )
                    SELECT EXISTS (SELECT 1 FROM inserted_sub);
                    """;
        var created = await dbConnection.QuerySingleAsync<bool>(query, new { chatId = subscription.Id.ChatId.Value, linkId = subscription.Id.LinkId.Value, tagNames = subscription.Tags.Select(t => t.Name).ToArray() }, transaction: dbSession.Transaction);

        return created;
    }

    public async Task<Subscription?> GetAsync(ChatId chatId, string url)
    {
        var dbConnection = await dbSession.OpenConnectionAsync();
        var query =
            """
            SELECT s.chat_id, s.link_id, t.name AS tag_name FROM subscriptions AS s
                INNER JOIN links AS l ON l.url = @url AND l.id = s.link_id
                LEFT JOIN tags AS t ON t.link_id = s.link_id AND t.chat_id = s.chat_id
            WHERE s.chat_id = @chatId
            """;
        var results = (await dbConnection.QueryAsync<(long chatId, Guid linkId, string? tagName)>(query, new { url, chatId = chatId.Value }, transaction: dbSession.Transaction)).ToList();
        if (results.Count == 0)
        {
            return null;
        }
        var linkId = results[0].linkId;
        var tags = results
            .Where(r => !string.IsNullOrEmpty(r.tagName))
            .Select(r => new Tag(r.tagName!));

        return new Subscription(
            new SubscriptionId(new LinkId(linkId), chatId),
            tags
        );
    }

    public async Task RemoveAsync(Subscription subscription)
    {
        var dbConnection = await dbSession.OpenConnectionAsync();
        var query = "DELETE FROM subscriptions WHERE chat_id = @chatId AND link_id = @linkId";
        await dbConnection.ExecuteAsync(query,
            new { chatId = subscription.Id.ChatId.Value, linkId = subscription.Id.LinkId.Value },
            transaction: dbSession.Transaction);
    }
}
