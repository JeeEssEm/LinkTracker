using System.Text.Json;
using LinkTracker.Bot.Contracts.Rest;
using LinkTracker.Scrapper.Application.Dtos.Outbox;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Bot;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using Microsoft.Extensions.Logging;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.NotificationStrategies;

public class SyncNotificationStrategy(IBotClient bot, ILogger<SyncNotificationStrategy> logger) : INotificationStrategy
{
    public async Task<SendResult> SendAsync(OutboxEvent[] events, CancellationToken ct)
    {
        var failed = new List<FailedToDeliver>();
        var deliveredIds = new List<long>();

        foreach (var outboxEvent in events)
        {
            try
            {
                var content = JsonSerializer.Deserialize<LinkUpdateRequest>(outboxEvent.Payload);
                if (content == null)
                {
                    logger.LogWarning("Content is {cntnt}", content);
                    failed.Add(new FailedToDeliver(outboxEvent.Id, "Content is null after deserialize"));
                    continue;
                }
                await bot.SendUpdateAsync(content); // TODO: распараллелить отправку
                deliveredIds.Add(outboxEvent.Id);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to send notification via sync");
                failed.Add(new FailedToDeliver(outboxEvent.Id, e.Message));
            }
        }

        return new SendResult(deliveredIds, failed);
    }
}
