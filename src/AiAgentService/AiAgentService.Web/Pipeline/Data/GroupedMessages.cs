using Confluent.Kafka;

namespace AiAgentService.Web.Pipeline.Data;

public record GroupedMessages(
    IReadOnlyList<long> ChatIds,
    string Description,
    string Priority,
    HashSet<TopicPartitionOffset> MessageIds);
