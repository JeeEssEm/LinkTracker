using LinkTracker.Scrapper.Application.Scraping.Pipelines;
using LinkTracker.Scrapper.Configurations;
using Microsoft.Extensions.Options;
using Quartz;

namespace LinkTracker.Scrapper.Scheduling;

public class LinkCheckJob(
    IOptions<SchedulerOptions> schedulerOptions,
    ScrapingCoordinator coordinator
    ) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        await coordinator.RunIterationAsync(schedulerOptions.Value.BatchSize);
    }
}
