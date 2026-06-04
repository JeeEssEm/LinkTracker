using System.Diagnostics;
using AiAgentService.Web.Metrics;
using Refit;

namespace AiAgentService.Web.Infrastructure.ExternalApi;

public class OpenAiClientWrapper(IOpenAiApiClient client, ApplicationMetrics metrics) : IOpenAiCompatibleApiClient
{
    public async Task<ApiResponse<ChatCompletionResponse>> CreateChatCompletionAsync(ChatCompletionRequest request)
    {
        var sw = Stopwatch.StartNew();
        var resp = await client.CreateChatCompletionAsync(request);
        sw.Stop();
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", "llm_agent"),
            new KeyValuePair<string, object?>("scope_type", request.Model)
        );

        return resp;
    }
}