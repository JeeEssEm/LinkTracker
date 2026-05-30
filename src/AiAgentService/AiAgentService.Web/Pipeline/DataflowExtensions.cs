using System.Threading.Tasks.Dataflow;
using AiAgentService.Web.Pipeline.Handlers.Abstractions;

namespace AiAgentService.Web.Pipeline;


public static class DataflowExtensions
{
    public static TransformBlock<TInput, TOutput> CreateScopedTransformBlock<TInput, TOutput, TStep>(
        IServiceScopeFactory scopeFactory,
        ExecutionDataflowBlockOptions options,
        ILogger logger) where TStep : IMessageHandler<TInput, TOutput> where TOutput : class
    {
        return new TransformBlock<TInput, TOutput>(async input =>
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var step = scope.ServiceProvider.GetRequiredService<TStep>();
                return await step.HandleAsync(input);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to process message");
                throw;
            }
        }, options);
    }
}
