using AiAgentService.Web.Pipeline.Data;
using Confluent.Kafka;

namespace AiAgentService.Web.Infrastructure;

public interface IConsumerUpdatesManager : IDisposable
{
    public MessageData? Consume(CancellationToken cancellationToken);
    public void Commit(IEnumerable<TopicPartitionOffset> messageIds, IEnumerable<long> chatIds);
}
