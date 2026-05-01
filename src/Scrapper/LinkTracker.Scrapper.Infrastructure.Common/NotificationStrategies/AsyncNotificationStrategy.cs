using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using LinkTracker.Bot.Contracts.Rest;
using LinkTracker.Contracts.Broker;
using LinkTracker.Scrapper.Application.Dtos.Outbox;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.NotificationStrategies;

public class AsyncNotificationStrategy(
    IOptions<NotificationStrategyOptions> options,
    IProducer<Null, string> producer,
    ILogger<AsyncNotificationStrategy> logger
    ) : INotificationStrategy
{
    public async Task<SendResult> SendAsync(OutboxEvent[] events, CancellationToken ct)
    {
        var tasks = events.ToDictionary(
            msg => SendToKafkaAsync(producer, msg, ct),
            msg => msg
        );

        var deliveredIds = new List<long>();
        var failed = new List<FailedToDeliver>();

        await foreach (var completedTask in Task.WhenEach(tasks.Keys).WithCancellation(ct))
        {
            var originalEvent = tasks[completedTask];
            try
            {
                var result = await completedTask;
                if (result.Status == PersistenceStatus.Persisted)
                {
                    deliveredIds.Add(originalEvent.Id);
                }
                else
                {
                    failed.Add(new FailedToDeliver(originalEvent.Id, "Not persisted"));
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Kafka send error");
                failed.Add(new FailedToDeliver(originalEvent.Id, e.Message));
            }
        }

        return new SendResult(deliveredIds, failed);
    }

    private Task<DeliveryResult<Null, string>> SendToKafkaAsync(IProducer<Null, string> producer, OutboxEvent ev, CancellationToken ct)
    {
        return producer.ProduceAsync(options.Value.Topic, new Message<Null, string>
        {
            Value = ev.Payload,
            Headers = CreateHeaders(ev.Headers)
        }, ct);
    }

    private static Headers CreateHeaders(Dictionary<string, string> headers)
    {
        var result = new Headers();
        foreach (var (key, value) in headers)
        {
            result.Add(key, Encoding.UTF8.GetBytes(value));
        }

        return result;
    }
}
