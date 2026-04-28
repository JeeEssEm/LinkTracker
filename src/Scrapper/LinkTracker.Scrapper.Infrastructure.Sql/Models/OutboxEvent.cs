using LinkTracker.Scrapper.Application.Dtos.Outbox;

namespace LinkTracker.Scrapper.Infrastructure.Sql.Models;

public class OutboxEvent
{
    public long Id { get; init; }
    public string Headers { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public int RetryCount { get; init; }
    public string ErrorMessages { get; init; } = string.Empty;
    public EventId EventId { get; init; }
    public EventStatus Status { get; init; }
    public DateTime AttemptedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
