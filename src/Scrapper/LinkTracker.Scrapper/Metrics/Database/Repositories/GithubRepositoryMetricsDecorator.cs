using System.Diagnostics;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.SourceHandlers.Github;

namespace LinkTracker.Scrapper.Metrics.Database.Repositories;

public class GithubRepositoryMetricsDecorator(IGithubRepository githubRepo, ApplicationMetrics metrics) : IGithubRepository
{
    public async Task CreateAsync(LinkId linkId, string name, string ownerName)
    {
        var sw = Stopwatch.StartNew();
        await githubRepo.CreateAsync(linkId, name, ownerName);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "github_repositories")
        );
    }
}