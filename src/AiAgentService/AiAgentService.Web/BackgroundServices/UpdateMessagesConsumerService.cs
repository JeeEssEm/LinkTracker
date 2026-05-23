using AiAgentService.Web.Configurations;
using AiAgentService.Web.Pipeline;
using AiAgentService.Web.Pipeline.Data;
using AiAgentService.Web.Pipeline.Handlers.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace AiAgentService.Web.BackgroundServices;

internal class UpdateMessagesConsumerService(
    ConsumerConfig consumerConfig,
    MessageProcessingPipeline pipeline,
    IOptions<KafkaConfiguration> kafkaConfig,
    ILogger<UpdateMessagesConsumerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = new ConsumerBuilder<Null, string>(consumerConfig).Build();
        consumer.Subscribe(kafkaConfig.Value.ConsumerTopic);
        await Task.Yield();
        try
        {
            logger.LogInformation("Started to listen kafka topic={topic}", kafkaConfig.Value.ConsumerTopic);
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(stoppingToken);
                logger.LogInformation("Consumed message {msg}", consumeResult.Message);
                await pipeline.PushMessage(consumeResult, consumer);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("OperationCanceledException caught");
        }
        finally
        {
            logger.LogInformation("Shutting down consumer service");
            await pipeline.Complete(); // сначала завершить пайплайн, чтоб consumer не утёк из-за MessageWrap, а потом consumer.Close
            consumer.Close();
        }
    }
}