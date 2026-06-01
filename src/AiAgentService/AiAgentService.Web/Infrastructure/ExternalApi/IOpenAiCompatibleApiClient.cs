using Refit;

namespace AiAgentService.Web.Infrastructure.ExternalApi;

public interface IOpenAiCompatibleApiClient
{
    Task<ApiResponse<ChatCompletionResponse>> CreateChatCompletionAsync(ChatCompletionRequest request);
}