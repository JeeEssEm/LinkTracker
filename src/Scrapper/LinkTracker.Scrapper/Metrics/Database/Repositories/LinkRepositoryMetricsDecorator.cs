using System.Diagnostics;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;

namespace LinkTracker.Scrapper.Metrics.Database.Repositories;

public class LinkRepositoryMetricsDecorator(ILinkRepository linkRepo, ApplicationMetrics metrics) : ILinkRepository
{
    public async Task<Link> GetOrCreateAsync(Uri uri, LinkSourceType sourceType)
    {
        var sw = Stopwatch.StartNew();
        var result = await linkRepo.GetOrCreateAsync(uri, sourceType);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "links")
        );
        return result;
    }

    public async Task<bool> RemoveLinkAsync(Uri uri)
    {
        var sw = Stopwatch.StartNew();
        var result = await linkRepo.RemoveLinkAsync(uri);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "links")
        );
        return result;
    }
}
