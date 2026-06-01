using System.Diagnostics;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scheduling;
using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;

namespace LinkTracker.Scrapper.Metrics.Database.Repositories;

public class LinkBulkRepositoryMetricsDecorator(ILinkBulkRepository linkBulkRepo, ApplicationMetrics metrics) : ILinkBulkRepository
{
    public async Task RemoveLinksAsync(IEnumerable<Guid> linkIds)
    {
        var sw = Stopwatch.StartNew();
        await linkBulkRepo.RemoveLinksAsync(linkIds);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "links")
        );
    }

    public async Task<List<LinkCheckEntry>> GetLinksToCheck(int page, int batchSize, DateTime now)
    {
        var sw = Stopwatch.StartNew();
        var result = await linkBulkRepo.GetLinksToCheck(page, batchSize, now);
        // Из ТЗ вообще неясно, что делать с запросами к нескольким таблицам
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "links")
        );
        return result;
    }

    public async Task UpdateCheckDatesAt(IEnumerable<Guid> linkIds)
    {
        var sw = Stopwatch.StartNew();
        await linkBulkRepo.UpdateCheckDatesAt(linkIds);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "links")
        );
    }

    public async Task<Dictionary<LinkSourceType, long>> GetNumberOfTrackedLinks()
    {
        var sw = Stopwatch.StartNew();
        var result = await linkBulkRepo.GetNumberOfTrackedLinks();
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "links")
        );
        return result;
    }
}
