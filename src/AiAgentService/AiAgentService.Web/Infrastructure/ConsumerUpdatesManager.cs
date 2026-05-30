using System.Text.Json;
using AiAgentService.Web.Configurations;
using AiAgentService.Web.Infrastructure.MessageAcknowledgment;
using AiAgentService.Web.Pipeline.Data;
using Confluent.Kafka;
using LinkTracker.Scrapper.Contracts.Dtos.Notifications;
using Microsoft.Extensions.Options;

namespace AiAgentService.Web.Infrastructure;

public class ConsumerUpdatesManager : IConsumerUpdatesManager
{
    private readonly ILogger<ConsumerUpdatesManager> _logger;
    private readonly MessageAcknowledgementManager _messagesManager;
    private readonly IConsumer<Null, string> _consumer;

    public ConsumerUpdatesManager(ConsumerConfig consumerConfig, IOptions<KafkaConfiguration> kafkaConfig, ILogger<ConsumerUpdatesManager> logger, MessageAcknowledgementManager messagesManager)
    {
        _logger = logger;
        _messagesManager = messagesManager;
        _consumer = new ConsumerBuilder<Null, string>(consumerConfig).Build();
        _consumer.Subscribe(kafkaConfig.Value.ConsumerTopic);
        logger.LogInformation("Started to listen kafka topic={topic}", kafkaConfig.Value.ConsumerTopic);
    }

    public MessageData? Consume(CancellationToken cancellationToken)
    {
        try
        {
            var consumeResult = _consumer.Consume(cancellationToken);
            _logger.LogInformation("Consumed message {msg}", consumeResult.Message);
            var dto = JsonSerializer.Deserialize<UpdateMessage>(consumeResult.Message.Value);
            if (dto == null)
            {
                _logger.LogError("Cannot deserialize dto! Received message: {msg}", consumeResult);
                return null;
            }
            _messagesManager.Update(dto, consumeResult.TopicPartitionOffset);

            return new MessageData(dto, consumeResult.TopicPartitionOffset);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed during receiving message");
            return null;
        }
    }

    public void Commit(IEnumerable<TopicPartitionOffset> messageIds, IEnumerable<long> chatIds)
    {
        var messagesToCommit = _messagesManager.Commit(messageIds, chatIds);
        if (messagesToCommit.Count > 0)
        {
            _consumer.Commit(messagesToCommit);
            _logger.LogInformation("Acknowledged messages with offsets={offsets}", messagesToCommit);
        }
    }

    public void Dispose()
    {
        _consumer.Close();
    }
}