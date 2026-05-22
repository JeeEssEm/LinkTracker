using LinkTracker.Scrapper.Application.Dtos.Outbox;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using Microsoft.Extensions.Logging;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.NotificationStrategies;

public class FallbackNotificationStrategy(
    SyncNotificationStrategy primary,
    AsyncNotificationStrategy fallback,
    ILogger<FallbackNotificationStrategy> logger
) : INotificationStrategy
{
    public async Task<SendResult> SendAsync(OutboxEvent[] events, CancellationToken ct)
    {
        var result = await primary.SendAsync(events, ct);

        if (result.FailedToDelivers.Count > 0)
        {
            logger.LogWarning("HTTP is not available, fallback to Kafka. Sent: {failed}/{all}", events.Length, result.FailedToDelivers.Count);
            var failedIds = result.FailedToDelivers.Select(e => e.Id).ToHashSet();
            var kafkaResult = await fallback.SendAsync(events.Where(e => failedIds.Contains(e.Id)).ToArray(), ct);

            var delivered = result.DeliveredIds.ToList();
            delivered.AddRange(kafkaResult.DeliveredIds);

            result = kafkaResult with { DeliveredIds = delivered };
        }

        return result;
    }
}
