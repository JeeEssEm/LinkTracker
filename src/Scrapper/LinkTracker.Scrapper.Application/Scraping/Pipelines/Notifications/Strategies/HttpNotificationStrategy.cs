using System.Threading;
using System.Threading.Tasks;
using LinkTracker.Bot.Contracts.Rest;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Bot;
using Microsoft.Extensions.Logging;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications.Strategies;

public class HttpNotificationStrategy(IBotClient bot, ILogger<HttpNotificationStrategy> logger) : INotificationStrategy
{
    public string TransportName => "Http";

    public Task SendAsync(NotificationMessage[] messages, CancellationToken ct)
    {
        logger.LogInformation("Sending {Count} messages via HTTP strategy.", messages.Length);
        foreach (var notificationMessage in messages)
        {
            bot.SendUpdateAsync(new LinkUpdateRequest(
                notificationMessage.LinkId, notificationMessage.Url,
                notificationMessage.Message, notificationMessage.UserIdsToNotify
            ));
        }

        return Task.CompletedTask;
    }
}