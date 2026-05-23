using AiAgentService.Web.Configurations;
using AiAgentService.Web.Pipeline.Data;
using AiAgentService.Web.Pipeline.Filters.Abstractions;
using Microsoft.Extensions.Options;

namespace AiAgentService.Web.Pipeline.Filters.BannedAuthors;

public class BannedAuthorsFilter(IOptions<FiltersConfiguration> config, ILogger<BannedAuthorsFilter> logger) : LoggingMessageFilter(logger)
{
    protected override bool CheckInvalid(MessageWrap message)
    {
        return config.Value.ExcludedAuthors.Contains(message.Message.Author);
    }
}
