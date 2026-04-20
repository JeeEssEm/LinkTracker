using Dapper;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Domain.Tag.Entities;
using LinkTracker.Scrapper.Infrastructure.Sql.ModelExtensions;

namespace LinkTracker.Scrapper.Infrastructure.Sql.Repositories;

public class TagRepository(DatabaseSession dbSession) : ITagRepository
{
    public async Task<List<Tag>> CreateTags(IEnumerable<string> tagNames, long chatId)
    {
        var dbConnection = await dbSession.GetConnectionAsync();

        var query =
            """
            INSERT INTO tags (name, chat_id)
                SELECT name, @chatId FROM UNNEST(@tagNames) AS name
            RETURNING *
            """;
        var tags = await dbConnection.QueryAsync<Models.Tag>(query, new { tagNames = tagNames.ToArray(), chatId }, transaction: dbSession.Transaction);
        return tags.ToDomain().ToList();
    }

    public async Task<List<Tag>> GetTags(long chatId, long linkId)
    {
        var dbConnection = await dbSession.GetConnectionAsync();
        var query =
            """
            SELECT t.* FROM tags AS t
            INNER JOIN link_tags AS lt ON
                lt.tag_id = t.id AND
                lt.link_id = @linkId AND
                t.chat_id = @chatId
            """;
        var tags = await dbConnection.QueryAsync<Models.Tag>(query, new { chatId, linkId }, transaction: dbSession.Transaction);
        return tags.ToDomain().ToList();
    }

    public async Task<List<Tag>> GetTags(long chatId, IEnumerable<string> tagNames)
    {
        var dbConnection = await dbSession.GetConnectionAsync();
        var query =
            """
            SELECT * FROM tags
            WHERE chat_id = @chatId AND name IN (SELECT * FROM UNNEST(@tagNames))
            """;
        var tags = await dbConnection.QueryAsync<Models.Tag>(
            query,
            new { chatId, tagNames = tagNames.ToArray() }, transaction: dbSession.Transaction);

        return tags.ToDomain().ToList();
    }

    public async Task ConnectTagsToLink(long linkId, IEnumerable<long> tagsIds)
    {
        var dbConnection = await dbSession.GetConnectionAsync();
        await dbConnection.ExecuteAsync(
            """
            INSERT INTO link_tags(link_id, tag_id) 
                SELECT @linkId, tag_id FROM UNNEST(@tagsIds) AS tag_id
            """, new { linkId, tagsIds = tagsIds.ToArray() }, transaction: dbSession.Transaction);
    }

    public async Task RemoveConnectionsTagsToLink(long chatId, long linkId)
    {
        var dbConnection = await dbSession.GetConnectionAsync();
        await dbConnection.ExecuteAsync(
            """
            DELETE FROM link_tags AS lt
            USING tags AS t
            WHERE lt.link_id = @linkId AND t.chat_id = @chatId AND lt.tag_id = t.id
            """, new { chatId, linkId }, transaction: dbSession.Transaction);
    }

    public async Task RemoveTags(IEnumerable<long> tagIds)
    {
        var dbConnection = await dbSession.GetConnectionAsync();
        await dbConnection.ExecuteAsync(
            """
            DELETE FROM tags
            WHERE id = ANY(@tagIds)
            """, new { tagIds = tagIds.ToArray() }, transaction: dbSession.Transaction);
    }
}