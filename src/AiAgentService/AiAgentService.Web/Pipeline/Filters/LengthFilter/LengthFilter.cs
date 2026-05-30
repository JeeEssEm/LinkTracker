using AiAgentService.Web.Configurations;
using AiAgentService.Web.Pipeline.Data;
using AiAgentService.Web.Pipeline.Filters.Abstractions;
using Microsoft.Extensions.Options;

namespace AiAgentService.Web.Pipeline.Filters.LengthFilter;

public class LengthFilter(IOptions<FiltersConfiguration> config, ILogger<LengthFilter> logger) : LoggingMessageFilter(logger)
{
    protected override bool CheckInvalid(MessageData message)
    {
        return message.Message.Description.Length < config.Value.MinLength;
    }
}