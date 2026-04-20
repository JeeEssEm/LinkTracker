using System.Text.Json.Serialization;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.ExternalApi.StackOverflow.Dtos;

public record QuestionsResponseDto(
    [property: JsonPropertyName("items")] List<QuestionDto> Questions,
    [property: JsonPropertyName("has_more")] bool HasMore
);
