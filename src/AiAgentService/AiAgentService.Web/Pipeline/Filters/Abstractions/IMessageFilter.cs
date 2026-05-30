using AiAgentService.Web.Pipeline.Data;

namespace AiAgentService.Web.Pipeline.Filters.Abstractions;

public interface IMessageFilter
{
    bool IsInvalid(MessageData message);
}
