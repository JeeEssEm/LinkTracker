using Confluent.Kafka;
using LinkTracker.Scrapper.Contracts.Dtos.Notifications;

namespace AiAgentService.Web.Pipeline.Data;

public record MessageWrap(
    UpdateMessage Message,
    TopicPartitionOffset Offset,
    IConsumer<Null, string> Consumer,
    bool IsFiltered = false);
