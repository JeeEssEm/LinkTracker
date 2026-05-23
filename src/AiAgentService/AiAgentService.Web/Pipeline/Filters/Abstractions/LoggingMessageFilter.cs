using AiAgentService.Web.Pipeline.Data;

namespace AiAgentService.Web.Pipeline.Filters.Abstractions;

public abstract class LoggingMessageFilter(ILogger logger) : IMessageFilter
{
    public bool IsInvalid(MessageWrap message)
    {
        var invalid = CheckInvalid(message);
        logger.LogInformation("Filter `{type}` with filtered={invalid} for message offset={offset}", GetType().Name, invalid, message.Offset.Offset);
        return invalid;
    }

    protected abstract bool CheckInvalid(MessageWrap message);
}