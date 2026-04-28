namespace LinkTracker.Scrapper.Infrastructure.Pipelines.Outbox;

public class OutboxConfiguration
{
    public const string Position = nameof(OutboxConfiguration);
    public int DelayBetweenPollsSeconds { get; init; } = 60;
    public int BatchSize { get; init; } = 100;
    public int RetryCountLimit { get; init; } = 3;
    public int MessageTimeoutMs { get; init; } = 5 * 1000; // 5 sec
}
