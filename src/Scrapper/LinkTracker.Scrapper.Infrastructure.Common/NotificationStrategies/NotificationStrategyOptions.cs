namespace LinkTracker.Scrapper.Infrastructure.Pipelines.NotificationStrategies;

public class NotificationStrategyOptions
{
    public const string Position = nameof(NotificationStrategyOptions);
    public NotificationStrategies Strategy { get; init; } = NotificationStrategies.Kafka;
    public string? Topic { get; init; }
    public string? BootstrapServers { get; init; }
    public bool FallbackToKafka { get; init; } = false;

    public void ValidateKafkaStrategy()
    {
        if (string.IsNullOrWhiteSpace(Topic))
        {
            throw new ArgumentException("Parameter `Topic` is not set for kafka notification strategy!");
        }

        if (string.IsNullOrWhiteSpace(BootstrapServers))
        {
            throw new ArgumentException("Parameter `BootstrapServers` is not set for kafka notification strategy!");
        }
    }
}
