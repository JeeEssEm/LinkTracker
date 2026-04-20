using Dapper;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store.ReadModels;
using LinkTracker.Scrapper.Infrastructure.Orm.Context;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Repositories;

public class OrmGithubStore(LinkTrackerContext context) : AbstractGithubStore
{
    protected override async Task<(long RepoId, int Number)[]> ExecuteInsertPairsAsync(string sql, object parameters)
    {
        var connection = context.Database.GetDbConnection();
        var transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }
        var result = await connection.QueryAsync<(long, int)>(sql, parameters, transaction);
        return result.ToArray();
    }

    protected override async Task<int> ExecuteAsync(string sql, object parameters)
    {
        var connection = context.Database.GetDbConnection();
        var transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }
        return await connection.ExecuteAsync(sql, parameters, transaction);
    }

    private record GithubRepoRow(int Id, long LinkId, string Owner, string Name);

    protected override async Task<Dictionary<long, RepositoryModel>> GetRepositoriesByLinkIdsAsyncInternal(IEnumerable<long> linkIds)
    {
        const string sql = "SELECT id AS Id, link_id AS LinkId, owner AS Owner, name AS Name FROM github_repositories WHERE link_id = ANY(@LinkIds)";
        var connection = context.Database.GetDbConnection();
        var transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        var results = await connection.QueryAsync<GithubRepoRow>(sql, new { LinkIds = linkIds.ToArray() }, transaction);
        return results.ToDictionary(
            x => x.LinkId,
            x => new RepositoryModel(x.Id, x.LinkId, x.Owner, x.Name)
        );
    }
}