using System.Threading;
using System.Threading.Tasks;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;

public interface INotificationStrategy
{
    string TransportName { get; }
    Task SendAsync(NotificationMessage[] messages, CancellationToken ct);
}
