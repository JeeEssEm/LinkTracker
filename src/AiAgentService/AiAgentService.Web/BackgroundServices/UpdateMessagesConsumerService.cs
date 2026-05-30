using AiAgentService.Web.Infrastructure;
using AiAgentService.Web.Pipeline;

namespace AiAgentService.Web.BackgroundServices;

internal class UpdateMessagesConsumerService(
    IConsumerUpdatesManager consumerManager,
    MessageProcessingPipeline pipeline,
    ILogger<UpdateMessagesConsumerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = consumerManager.Consume(stoppingToken);
                if (consumeResult != null)
                {
                    await pipeline.PushMessage(consumeResult);
                }
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
        }
    }
}