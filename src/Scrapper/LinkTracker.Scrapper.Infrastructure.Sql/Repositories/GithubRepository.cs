using Dapper;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.SourceHandlers.Github;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Database;

namespace LinkTracker.Scrapper.Infrastructure.Sql.Repositories;

public class GithubRepository(DatabaseSession dbSession) : IGithubRepository
{
    public async Task CreateAsync(LinkId linkId, string name, string ownerName)
    {
        var dbConnection = await dbSession.OpenConnectionAsync();
        var query =
            """
            INSERT INTO github_repositories(link_id, name, owner) VALUES (@linkId, @name, @ownerName)
            ON CONFLICT DO NOTHING
            """;
        await dbConnection.ExecuteAsync(query, new { linkId = linkId.Value, name, ownerName }, transaction: dbSession.Transaction);
    }
}
