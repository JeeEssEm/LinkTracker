using Refit;

namespace AiAgentService.Web.Infrastructure.ExternalApi;

public record ChatMessage()
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
};

public class ChatCompletionRequest
{
    public string Model { get; set; } = string.Empty;
    public List<ChatMessage> Messages { get; set; } = [];
    public bool Stream { get; set; } = false;
}

public class ChatChoice
{
    public ChatMessage Message { get; set; } = new();
    public string FinishReason { get; set; } = string.Empty;
}

public class ChatCompletionResponse
{
    public string Id { get; set; } = string.Empty;
    public List<ChatChoice> Choices { get; set; } = [];
}

public interface IOpenAiApiClient
{
    [Post("/api/v1/chat/completions")]
    Task<ApiResponse<ChatCompletionResponse>> CreateChatCompletionAsync([Body] ChatCompletionRequest request);
}
