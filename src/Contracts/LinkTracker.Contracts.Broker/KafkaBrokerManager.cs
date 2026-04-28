using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace LinkTracker.Contracts.Broker;

public static class KafkaBrokerManager
{
    public static async Task EnsureTopicExistsAsync(TopicConfiguration config, string topicName, string bootstrapServers)
    {
        var adminConfig = new AdminClientConfig() { BootstrapServers = bootstrapServers };

        using var adminClient = new AdminClientBuilder(adminConfig).Build();
        try
        {
            var topicSpec = new TopicSpecification
            {
                Name = topicName,
                ReplicationFactor = config.ReplicationFactor,
                NumPartitions = config.NumPartitions,
                Configs = new Dictionary<string, string>
                {
                    { "min.insync.replicas", config.InSyncReplicas.ToString() },
                    { "retention.ms", config.RetentionMilliseconds.ToString() },
                    { "retention.bytes", config.RetentionBytes.ToString() },
                    { "cleanup.policy", config.CleanupPolicy == CleanupPolicy.Delete ? "delete" : "compact" }
                }
            };

            await adminClient.CreateTopicsAsync([topicSpec]);

        }
        catch (CreateTopicsException e)
        {
            if (e.Results.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
            {
                return;
            }

            throw;
        }
    }
}
