using System.Text.Json;

namespace LinkTracker.Scrapper.Application.Dtos.Outbox;

public record CreateOutboxEvent
{
    public EventId EventId { get; init; }
    public EventStatus Status = EventStatus.Pending;
    public string Headers { get; init; } = "{}";
    public string Payload { get; init; }

    public CreateOutboxEvent(object payload)
    {
        Payload = JsonSerializer.Serialize(payload);
    }

    public CreateOutboxEvent(object payload, Dictionary<string, string> headers)
    {
        Payload = JsonSerializer.Serialize(payload);
        Headers = JsonSerializer.Serialize(headers);
    }
}
