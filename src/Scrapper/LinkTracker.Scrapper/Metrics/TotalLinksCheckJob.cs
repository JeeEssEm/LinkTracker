using LinkTracker.Scrapper.Application.Repositories;
using Quartz;

namespace LinkTracker.Scrapper.Metrics;

public class TotalLinksCheckJob(ApplicationMetrics metrics, ILinkBulkRepository linkRepo) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var links = await linkRepo.GetNumberOfTrackedLinks();
        foreach (var (source, count) in links)
        {
            metrics.MonitoredLinks.Record(count, new KeyValuePair<string, object?>("tracked_source", source.ToString().ToLower()));
        }
    }
}