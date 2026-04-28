namespace LinkTracker.Scrapper.Application.Dtos.Outbox;

public enum EventStatus
{
    Pending = 0,
    Processing = 1,
    Sent = 2,
    Failed = 3
}
