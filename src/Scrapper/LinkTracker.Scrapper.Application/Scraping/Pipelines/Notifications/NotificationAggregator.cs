using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using LinkTracker.Scrapper.Application.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;

public class NotificationAggregator
{
    private readonly ILogger<NotificationAggregator> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly BatchBlock<NotificationMessage> _batchBlock;

    public ITargetBlock<NotificationMessage> InputBlock { get; }

    public NotificationAggregator(ILogger<NotificationAggregator> logger,
        IServiceProvider serviceProvider,
        int batchSize = 10)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        _batchBlock = new BatchBlock<NotificationMessage>(batchSize);

        var notifyBlock = new ActionBlock<NotificationMessage[]>(NotifyUsers);

        _batchBlock.LinkTo(notifyBlock);

        InputBlock = _batchBlock;
    }

    private async Task NotifyUsers(NotificationMessage[] messages)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var strategy = scope.ServiceProvider.GetRequiredService<INotificationStrategy>();

        try
        {
            _logger.LogInformation("Агрегатор собрал батч из {count} уведомлений. Отправляем через {transport}...", messages.Length, strategy.TransportName);
            await strategy.SendAsync(messages, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending notifications using strategy {TransportName}", strategy.TransportName);
        }
    }


    public void Trigger()
    {
        if (_batchBlock.BatchSize > 0)
        {
            _batchBlock.TriggerBatch();
        }
    }
}