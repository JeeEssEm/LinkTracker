using System.Diagnostics;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store.ReadModels;

namespace LinkTracker.Scrapper.Metrics.Database.Repositories;

public class GithubStoreMetricsDecorator(IGithubStore githubStore, ApplicationMetrics metrics) : IGithubStore
{
    public async Task<IssueData[]> CreateAndReturnIfNotExist(IssueData[] issues)
    {
        var sw = Stopwatch.StartNew();
        var result = await githubStore.CreateAndReturnIfNotExist(issues);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "github_issues")
        );
        return result;
    }

    public async Task<PullRequestData[]> CreateAndReturnIfNotExist(PullRequestData[] pullRequests)
    {
        var sw = Stopwatch.StartNew();
        var result = await githubStore.CreateAndReturnIfNotExist(pullRequests);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "github_pull_requests")
        );
        return result;
    }

    public async Task<Dictionary<Guid, RepositoryModel>> GetRepositoriesByLinkIdsAsync(IEnumerable<Guid> linkIds)
    {
        var sw = Stopwatch.StartNew();
        var result = await githubStore.GetRepositoriesByLinkIdsAsync(linkIds);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "github_repositories")
        );
        return result;
    }
}
