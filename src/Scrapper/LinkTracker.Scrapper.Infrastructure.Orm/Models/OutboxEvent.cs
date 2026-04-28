using LinkTracker.Scrapper.Application.Dtos.Outbox;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Models;

public class OutboxEvent
{
    public long Id { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public required string Payload { get; set; }

    public int RetryCount { get; set; } = 0;
    public List<string> ErrorMessages { get; set; } = new();
    public EventId EventId { get; set; }
    public EventStatus Status = EventStatus.Pending;

    public DateTimeOffset AttemptedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
