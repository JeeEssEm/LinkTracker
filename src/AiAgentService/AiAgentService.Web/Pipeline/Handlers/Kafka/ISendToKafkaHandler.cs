namespace AiAgentService.Web.Pipeline.Handlers.Kafka;

public interface ISendToKafkaHandler
{
    Task<bool> Handle(object message);
}