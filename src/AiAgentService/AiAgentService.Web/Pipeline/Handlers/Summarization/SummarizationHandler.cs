using AiAgentService.Web.Configurations;
using AiAgentService.Web.Infrastructure.ExternalApi;
using AiAgentService.Web.Pipeline.Data;
using AiAgentService.Web.Pipeline.Handlers.Abstractions;
using Microsoft.Extensions.Options;

namespace AiAgentService.Web.Pipeline.Handlers.Summarization;

public class SummarizationHandler(IOptions<FiltersConfiguration> config, IOpenAiApiClient apiClient, ILogger<SummarizationHandler> logger) : IMessageHandler<MessageWrap, MessageWrap>
{
    public async Task<MessageWrap> HandleAsync(MessageWrap data)
    {
        try
        {
            if (data.Message.Description.Length >= config.Value.Summarization.Threshold)
            {
                var summarizedMessage = data.Message with { Description = await Summarize(data.Message.Description) };
                logger.LogDebug("summarized text for message offset={offset}", data.Offset.Offset);
                return data with { Message = summarizedMessage };
            }
            logger.LogDebug("summarized skipped for message offset={offset}", data.Offset.Offset);
            return data;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Something went wrong during summarization");
            return data;
        }
    }

    private async Task<string> Summarize(string text)
    {
        var fallbackText = text[..config.Value.Summarization.Threshold] + "...";
        if (!config.Value.Summarization.AiSummarizationEnabled)
        {
            return fallbackText;
        }

        var prompt = $"{config.Value.Summarization.SummarizationPrompt}\n{text}";
        var response = await apiClient.CreateChatCompletionAsync(new ChatCompletionRequest()
        {
            Model = config.Value.Summarization.Model,
            Messages = [new ChatMessage { Content = prompt, Role = "user" }]
        });

        if (!response.IsSuccessStatusCode || response.Content == null)
        {
            logger.LogWarning("Failed to summarize using LLM: status_code={status}, reason={err}", response.StatusCode, response.ReasonPhrase);
            return fallbackText;
        }

        var summarizedText = response.Content.Choices.FirstOrDefault()?.Message.Content;
        if (summarizedText == null)
        {
            logger.LogWarning("Failed to summarize using LLM with successful status");
            return fallbackText;
        }

        return summarizedText;
    }
}
