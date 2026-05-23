using AiAgentService.Web.Configurations;
using AiAgentService.Web.Pipeline.Data;
using AiAgentService.Web.Pipeline.Filters.Abstractions;
using Google.Protobuf;
using Microsoft.Extensions.Options;

namespace AiAgentService.Web.Pipeline.Filters.StopWords;

public class StopWordsFilter(IOptions<FiltersConfiguration> config, ILogger<StopWordsFilter> logger) : LoggingMessageFilter(logger)
{
    protected override bool CheckInvalid(MessageWrap message)
    {
        return config.Value.StopWords.Any(stopWord => message.Message.Description.Contains(stopWord));
    }
}
