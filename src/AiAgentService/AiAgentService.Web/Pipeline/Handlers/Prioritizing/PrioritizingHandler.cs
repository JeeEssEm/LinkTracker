using AiAgentService.Web.Configurations;
using AiAgentService.Web.Infrastructure.ExternalApi;
using AiAgentService.Web.Pipeline.Data;
using AiAgentService.Web.Pipeline.Handlers.Abstractions;
using Microsoft.Extensions.Options;

namespace AiAgentService.Web.Pipeline.Handlers.Prioritizing;

public class PrioritizingHandler(IOptions<PrioritizationConfiguration> config, IOpenAiCompatibleApiClient apiClient, ILogger<PrioritizingHandler> logger) : IMessageHandler<MessageData, MessageData>
{
    public async Task<MessageData> HandleAsync(MessageData data)
    {
        if (config.Value.AiPrioritizationEnabled)
        {
            logger.LogInformation("Prioritize with AI for id={msg_id}", data.Message.Id);
            return data with { Priority = await GetMessagePriority(data.Message.Description) };
        }
        logger.LogInformation("Prioritize without AI for id={msg_id}", data.Message.Id);
        return data with { Priority = GetFallbackMessagePriority(data.Message.Description) };
    }

    public async Task<MessagePriority> GetMessagePriority(string description)
    {
        var fallbackPriority = GetFallbackMessagePriority(description);
        var systemPrompt = string.Format(
            config.Value.Prompt,
            string.Join("\n", config.Value.LowKeywords.Select(w => $"- {w}")),
            string.Join("\n", config.Value.HighKeywords.Select(w => $"- {w}"))
        );
        var userPrompt = $"Define priority of this text:\n\n{description}";
        var response = await apiClient.CreateChatCompletionAsync(new ChatCompletionRequest()
        {
            Model = config.Value.Model,
            Messages = [
                new ChatMessage()
                {
                    Content = systemPrompt,
                    Role = "system"
                },
                new ChatMessage()
                {
                    Content = userPrompt,
                    Role = "user"
                }
            ]
        });
        if (!response.IsSuccessStatusCode || response.Content == null)
        {
            logger.LogWarning("Failed to summarize using LLM: status_code={status}, reason={err}", response.StatusCode, response.ReasonPhrase);
            return fallbackPriority;
        }
        var priority = response.Content.Choices.FirstOrDefault()?.Message.Content;
        switch (priority)
        {
            case "Low":
                return MessagePriority.Low;
            case "Medium":
                return MessagePriority.Medium;
            case "High":
                return MessagePriority.High;
            default:
                logger.LogWarning("Cannot parse LLM result: {res}. Used fallback: {fallback}", priority, fallbackPriority);
                return fallbackPriority;
        }
    }

    private MessagePriority GetFallbackMessagePriority(string description)
    {
        var priority = MessagePriority.Medium;
        if (config.Value.LowKeywords.Any(description.Contains))
        {
            priority = MessagePriority.Low;
        }
        if (config.Value.HighKeywords.Any(description.Contains))
        {
            priority = MessagePriority.High;
        }

        return priority;
    }
}