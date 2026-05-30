using System.Text.Json;
using AiAgentService.Web.Configurations;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace AiAgentService.Web.Pipeline.Handlers.Kafka;

public class SendToKafkaHandler(IProducer<Null, string> producer, IOptions<KafkaConfiguration> config) : ISendToKafkaHandler
{
    public async Task<bool> Handle(object message)
    {
        var result = await producer.ProduceAsync(config.Value.ProducerTopic, new Message<Null, string>()
        {
            Value = JsonSerializer.Serialize(message)
        });
        return result.Status != PersistenceStatus.NotPersisted;
    }
}
