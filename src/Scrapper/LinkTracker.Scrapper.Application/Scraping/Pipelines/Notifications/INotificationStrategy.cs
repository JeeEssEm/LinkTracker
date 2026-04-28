using System.Threading;
using System.Threading.Tasks;
using LinkTracker.Scrapper.Application.Dtos.Outbox;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;

public interface INotificationStrategy
{
    Task<SendResult> SendAsync(OutboxEvent[] events, CancellationToken ct);
}

public record SendResult(IReadOnlyList<long> DeliveredIds, IReadOnlyList<FailedToDeliver> FailedToDelivers);

public record FailedToDeliver(long Id, string Error);
