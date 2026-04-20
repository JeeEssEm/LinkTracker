using Dapper;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store.ReadModels;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Repositories;

namespace LinkTracker.Scrapper.Infrastructure.Sql.Repositories;

internal class SqlGithubStore(DatabaseSession session) : AbstractGithubStore
{
    protected override async Task<(long RepoId, int Number)[]> ExecuteInsertPairsAsync(string sql, object parameters)
    {
        var connection = await session.GetConnectionAsync();
        var result = await connection.QueryAsync<(long, int)>(sql, parameters, transaction: session.Transaction);
        return result.ToArray();
    }

    protected override async Task<int> ExecuteAsync(string sql, object parameters)
    {
        var connection = await session.GetConnectionAsync();
        return await connection.ExecuteAsync(sql, parameters, transaction: session.Transaction);
    }

    private record GithubRepoRow(int Id, long LinkId, string Owner, string Name);

    protected override async Task<Dictionary<long, RepositoryModel>> GetRepositoriesByLinkIdsAsyncInternal(IEnumerable<long> linkIds)
    {
        const string sql = "SELECT id, link_id, owner, name FROM github_repositories WHERE link_id = ANY(@LinkIds)";
        var connection = await session.GetConnectionAsync();

        var results = await connection.QueryAsync<GithubRepoRow>(sql, new { LinkIds = linkIds.ToArray() }, transaction: session.Transaction);

        return results.ToDictionary(
            x => x.LinkId,
            x => new RepositoryModel(x.Id, x.LinkId, x.Owner, x.Name)
        );
    }
}