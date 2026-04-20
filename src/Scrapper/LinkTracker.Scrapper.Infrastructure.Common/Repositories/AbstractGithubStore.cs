using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store.ReadModels;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.Repositories;

public abstract class AbstractGithubStore : IGithubStore
{
    private const string InsertIssuesSql = """
        INSERT INTO github_issues (github_repo_id, number, created_at, previous_status)
        SELECT * FROM UNNEST(@RepoIds, @Numbers, @CreatedAts, @Statuses)
        ON CONFLICT (github_repo_id, number) DO NOTHING
        RETURNING github_repo_id, number;
        """;

    private const string InsertPullRequestsSql = """
        INSERT INTO github_pull_requests (github_repo_id, number, created_at, previous_state)
        SELECT * FROM UNNEST(@RepoIds, @Numbers, @CreatedAts, @States)
        ON CONFLICT (github_repo_id, number) DO NOTHING
        RETURNING github_repo_id, number;
        """;

    private const string DeleteRepositorySql = """
        DELETE FROM github_repositories
        WHERE id = @Id;
        """;

    private const string CreateRepositorySql = """
        INSERT INTO github_repositories(link_id, name, owner) VALUES (@linkId, @name, @owner)
        ON CONFLICT DO NOTHING; 
        """;

    protected abstract Task<(long RepoId, int Number)[]> ExecuteInsertPairsAsync(string sql, object parameters);
    protected abstract Task<int> ExecuteAsync(string sql, object parameters);
    protected abstract Task<Dictionary<long, RepositoryModel>> GetRepositoriesByLinkIdsAsyncInternal(IEnumerable<long> linkIds);

    public Task<Dictionary<long, RepositoryModel>> GetRepositoriesByLinkIdsAsync(IEnumerable<long> linkIds)
    {
        return GetRepositoriesByLinkIdsAsyncInternal(linkIds);
    }

    public Task CreateRepository(long linkId, string name, string owner)
    {
        return ExecuteAsync(CreateRepositorySql, new { linkId, name, owner });
    }

    public async Task<IssueData[]> CreateAndReturnIfNotExist(IssueData[] issues)
    {
        if (issues.Length == 0)
        {
            return [];
        }

        var insertedPairs = (await ExecuteInsertPairsAsync(InsertIssuesSql, new
        {
            RepoIds = issues.Select(i => i.Repository.Id).ToArray(),
            Numbers = issues.Select(i => i.Number).ToArray(),
            CreatedAts = issues.Select(i => i.CreatedAt).ToArray(),
            Statuses = issues.Select(i => i.Status.ToString()).ToArray()
        })).ToHashSet();

        return issues.Where(i => insertedPairs.Contains((i.Repository.Id, i.Number))).ToArray();
    }

    public async Task<PullRequestData[]> CreateAndReturnIfNotExist(PullRequestData[] pullRequests)
    {
        if (pullRequests.Length == 0)
        {
            return [];
        }

        var insertedPairs = (await ExecuteInsertPairsAsync(InsertPullRequestsSql, new
        {
            RepoIds = pullRequests.Select(pr => pr.Repository.Id).ToArray(),
            Numbers = pullRequests.Select(pr => pr.Number).ToArray(),
            CreatedAts = pullRequests.Select(pr => pr.CreatedAt).ToArray(),
            States = pullRequests.Select(pr => pr.Status.ToString()).ToArray()
        })).ToHashSet();

        return pullRequests.Where(pr => insertedPairs.Contains((pr.Repository.Id, pr.Number))).ToArray();
    }

    public async Task<bool> RemoveRepositoryAsync(long id)
    {
        var rowsAffected = await ExecuteAsync(DeleteRepositorySql, new { Id = id });
        return rowsAffected > 0;
    }
}
