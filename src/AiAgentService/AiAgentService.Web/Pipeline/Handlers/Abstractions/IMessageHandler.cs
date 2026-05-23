namespace AiAgentService.Web.Pipeline.Handlers.Abstractions;

public interface IMessageHandler<in TIn, TOut>
{
    public Task<TOut> HandleAsync(TIn data);
}
