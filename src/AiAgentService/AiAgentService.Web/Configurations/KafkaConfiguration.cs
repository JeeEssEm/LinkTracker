using Confluent.Kafka;

namespace AiAgentService.Web.Configurations;

public class KafkaConfiguration
{
    public const string Position = nameof(KafkaConfiguration);

    public required string ProducerTopic { get; set; }
    public required string ConsumerTopic { get; set; }
    public required string BootstrapServers { get; set; }
    public required string GroupId { get; set; }
    public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Latest;
    public Acks Acks { get; set; } = Acks.Leader;
}
