using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using LinkTracker.Bot.Contracts.Rest;
using LinkTracker.Contracts.Broker;
using LinkTracker.Scrapper.Application.Abstractions;
using LinkTracker.Scrapper.Application.Dtos.Outbox;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using LinkTracker.Scrapper.Infrastructure.Pipelines.NotificationStrategies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.Outbox;

public class OutboxBackgroundService(
    IServiceProvider serviceProvider,
    IOptions<OutboxConfiguration> outboxConfig,
    ILogger<OutboxBackgroundService> logger
    ) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxEventRepository>();
            var strategy = scope.ServiceProvider.GetRequiredService<INotificationStrategy>();

            var messages = await outboxRepo.GetEventsToProcess(outboxConfig.Value.BatchSize, outboxConfig.Value.RetryCountLimit);
            if (messages.Length != 0)
            {
                var res = await strategy.SendAsync(messages, stoppingToken);
                await outboxRepo.SetSent(res.DeliveredIds);
                await outboxRepo.SetFailed(res.FailedToDelivers.Select(e => ValueTuple.Create(e.Id, e.Error)));
            }

            await Task.Delay(outboxConfig.Value.DelayBetweenPollsSeconds * 1000, stoppingToken);
        }
    }
}
