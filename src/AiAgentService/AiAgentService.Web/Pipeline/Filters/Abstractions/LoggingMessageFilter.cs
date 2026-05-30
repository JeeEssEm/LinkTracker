using AiAgentService.Web.Pipeline.Data;

namespace AiAgentService.Web.Pipeline.Filters.Abstractions;

public abstract class LoggingMessageFilter(ILogger logger) : IMessageFilter
{
    public bool IsInvalid(MessageData message)
    {
        var invalid = CheckInvalid(message);
        logger.LogInformation("Filter `{type}` with filtered={invalid} for message id={id}", GetType().Name, invalid, message.Message.Id);
        return invalid;
    }

    protected abstract bool CheckInvalid(MessageData message);
}