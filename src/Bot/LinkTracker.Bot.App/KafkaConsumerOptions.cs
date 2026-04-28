namespace LinkTracker.Bot.App;

public class KafkaConsumerOptions
{
    public const string Position = nameof(KafkaConsumerOptions);
    public bool KafkaConsumerEnabled { get; init; } = true;
    public required string Topic { get; init; }
    public required string BootstrapServers { get; set; }
    public required string GroupId { get; set; }
}
