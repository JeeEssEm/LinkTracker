using Dapper;
using LinkTracker.Scrapper.Application.QueryServices;
using LinkTracker.Scrapper.Contracts.Dtos.Links;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Database;

namespace LinkTracker.Scrapper.Infrastructure.Sql.QueryServices;

public class SubscriptionQueryService(DatabaseSession dbSession) : ISubscriptionQueryService
{
    public async Task<IReadOnlyList<LinkResponse>> ListSubscriptionsByChatAsync(ChatId chatId)
    {
        var dbConnection = await dbSession.OpenConnectionAsync();
        var query =
            """
            SELECT s.link_id, l.url, t.name AS tag_name FROM subscriptions AS s
                 INNER JOIN links AS l ON l.id = s.link_id
                 LEFT JOIN tags AS t ON t.link_id = s.link_id AND t.chat_id = s.chat_id
             WHERE s.chat_id = @chatId
            """;
        var result = new Dictionary<Guid, (string url, List<string> tags)>();
        await dbConnection.QueryAsync<(Guid linkId, string url), string, (string url, List<string> tags)>(query, (link, tagName) =>
        {
            var tags = new List<string>();
            if (!string.IsNullOrEmpty(tagName))
            {
                tags.Add(tagName);
            }
            if (result.TryGetValue(link.linkId, out (string url, List<string> tags) rec))
            {
                rec.tags.AddRange(tags);
            }
            else
            {
                result.Add(link.linkId, ValueTuple.Create(link.url, tags));
            }

            return result[link.linkId];
        }, new { chatId = chatId.Value }, splitOn: "tag_name");

        return result.Select(kvp => new LinkResponse(kvp.Key, kvp.Value.url, kvp.Value.tags.AsReadOnly())).ToList().AsReadOnly();
    }
}
