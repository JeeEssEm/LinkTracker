using AiAgentService.Web.Pipeline.Data;
using AiAgentService.Web.Pipeline.Filters.Abstractions;

namespace AiAgentService.Web.Pipeline.Filters;

public class AggregateFilter(IEnumerable<IMessageFilter> filters)
{
    private readonly IMessageFilter[] _filters = filters.ToArray();

    public MessageWrap Handle(MessageWrap message)
    {
        if (_filters.Any(messageFilter => messageFilter.IsInvalid(message)))
        {
            return message with { IsFiltered = true };
        }

        return message;
    }
}