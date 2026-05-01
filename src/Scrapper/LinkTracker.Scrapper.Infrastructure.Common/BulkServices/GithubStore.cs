using Dapper;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store.ReadModels;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Database;


namespace LinkTracker.Scrapper.Infrastructure.Pipelines.BulkServices;

public class GithubStore(DatabaseSession dbSession) : IGithubStore
{
    public async Task<IssueData[]> CreateAndReturnIfNotExist(IssueData[] issues)
    {
        if (issues.Length == 0)
        {
            return [];
        }

        var query =
            """
            INSERT INTO github_issues (github_repo_id, number, created_at, previous_status)
            SELECT * FROM UNNEST(@RepoIds, @Numbers, @CreatedAts, @Statuses)
            ON CONFLICT (github_repo_id, number) DO NOTHING
            RETURNING github_repo_id, number;
            """;

        var dbConnection = await dbSession.OpenConnectionAsync();

        var result = await dbConnection.QueryAsync<(long, int)>(query,
            new { RepoIds = issues.Select(i => i.Repository.Id).ToArray(), Numbers = issues.Select(i => i.Number).ToArray(), CreatedAts = issues.Select(i => i.CreatedAt).ToArray(), Statuses = issues.Select(i => i.Status.ToString()).ToArray() }, transaction: dbSession.Transaction);
        var insertedPairs = result.ToHashSet();
        return issues.Where(i => insertedPairs.Contains((i.Repository.Id, i.Number))).ToArray();
    }

    public async Task<PullRequestData[]> CreateAndReturnIfNotExist(PullRequestData[] pullRequests)
    {
        if (pullRequests.Length == 0)
        {
            return [];
        }

        var query =
            """
            INSERT INTO github_pull_requests (github_repo_id, number, created_at, previous_state)
            SELECT * FROM UNNEST(@RepoIds, @Numbers, @CreatedAts, @States)
            ON CONFLICT (github_repo_id, number) DO NOTHING
            RETURNING github_repo_id, number;
            """;
        var dbConnection = await dbSession.OpenConnectionAsync();
        var result = await dbConnection.QueryAsync<(long, int)>(query,
            new { RepoIds = pullRequests.Select(pr => pr.Repository.Id).ToArray(), Numbers = pullRequests.Select(pr => pr.Number).ToArray(), CreatedAts = pullRequests.Select(pr => pr.CreatedAt).ToArray(), States = pullRequests.Select(pr => pr.Status.ToString()).ToArray() },
            transaction: dbSession.Transaction);
        var insertedPairs = result.ToHashSet();

        return pullRequests.Where(pr => insertedPairs.Contains((pr.Repository.Id, pr.Number))).ToArray();
    }

    public async Task<Dictionary<Guid, RepositoryModel>> GetRepositoriesByLinkIdsAsync(IEnumerable<Guid> linkIds)
    {
        var query = "SELECT id, link_id, owner, name FROM github_repositories WHERE link_id = ANY(@LinkIds)";
        var connection = await dbSession.OpenConnectionAsync();

        var results = await connection.QueryAsync<(int Id, Guid LinkId, string Owner, string Name)>(
            query,
            new { LinkIds = linkIds.ToArray() }, transaction: dbSession.Transaction);

        return results.ToDictionary(
            x => x.LinkId,
            x => new RepositoryModel(x.Id, x.Owner, x.Name)
        );
    }
}