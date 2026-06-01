using System.Diagnostics;
using LinkTracker.Scrapper.Application.Dtos.Outbox;
using LinkTracker.Scrapper.Application.Repositories;

namespace LinkTracker.Scrapper.Metrics.Database.Repositories;

public class OutboxEventRepositoryMetricsDecorator(IOutboxEventRepository outboxEventRepo, ApplicationMetrics metrics) : IOutboxEventRepository
{
    public async Task CreateAsync(IEnumerable<CreateOutboxEvent> outboxEvent)
    {
        var sw = Stopwatch.StartNew();
        await outboxEventRepo.CreateAsync(outboxEvent);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "outbox_events")
        );
    }

    public async Task SetSent(IEnumerable<long> ids)
    {
        var sw = Stopwatch.StartNew();
        await outboxEventRepo.SetSent(ids);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "outbox_events")
        );
    }

    public async Task SetFailed(IEnumerable<(long eventId, string error)> events)
    {
        var sw = Stopwatch.StartNew();
        await outboxEventRepo.SetFailed(events);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "outbox_events")
        );
    }

    public async Task<OutboxEvent[]> GetEventsToProcess(int batchSize, int retryCountLimit, TimeSpan? retryDelay = null)
    {
        var sw = Stopwatch.StartNew();
        var result = await outboxEventRepo.GetEventsToProcess(batchSize, retryCountLimit, retryDelay);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "outbox_events")
        );
        return result;
    }
}
