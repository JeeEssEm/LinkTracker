namespace LinkTracker.Contracts.Broker;

public class TopicConfiguration
{
    public const string Position = nameof(TopicConfiguration);
    public int NumPartitions { get; init; } = 3;
    public short ReplicationFactor { get; init; } = 3;
    public int InSyncReplicas { get; init; } = 2;
    public int RetentionMilliseconds { get; init; } = 3 * 24 * 60 * 60 * 1000; // 3 days
    public int RetentionBytes { get; init; } = 1 * 1024 * 1024 * 1024; // 1 Gb
    public CleanupPolicy CleanupPolicy { get; init; } = CleanupPolicy.Delete;
}


public enum CleanupPolicy
{
    Delete = 0,
    Compress = 1
}
