namespace LinkTracker.Scrapper.Application.Dtos.Outbox;

public record OutboxEvent(
    long Id,
    Dictionary<string, string> Headers,
    string Payload,
    int RetryCount,
    List<string> ErrorMessages,
    EventId EventId,
    EventStatus Status,
    DateTime AttemptedAt,
    DateTime CreatedAt
);
