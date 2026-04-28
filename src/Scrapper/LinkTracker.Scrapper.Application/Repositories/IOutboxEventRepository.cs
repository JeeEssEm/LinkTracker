using LinkTracker.Scrapper.Application.Dtos.Outbox;

namespace LinkTracker.Scrapper.Application.Repositories;

public interface IOutboxEventRepository
{
    Task CreateAsync(IEnumerable<CreateOutboxEvent> outboxEvent);

    Task SetSent(IEnumerable<long> ids);
    Task SetFailed(IEnumerable<(long eventId, string error)> events);

    /// <summary>
    /// Меняет статус на Processing и attempt_at на now в БД,
    /// </summary>
    /// <returns>Массив событий для обработки</returns>
    Task<OutboxEvent[]> GetEventsToProcess(int batchSize, int retryCountLimit, TimeSpan? retryDelay = null);
}
