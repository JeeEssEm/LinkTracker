using Confluent.Kafka;
using LinkTracker.Scrapper.Contracts.Dtos.Notifications;

namespace AiAgentService.Web.Pipeline.Data;

public record MessageData(
    UpdateMessage Message,
    TopicPartitionOffset Offset,
    MessagePriority Priority = MessagePriority.Low,
    bool IsFiltered = false);
