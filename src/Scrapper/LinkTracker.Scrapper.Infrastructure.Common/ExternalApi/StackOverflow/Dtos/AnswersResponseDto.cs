using System.Text.Json.Serialization;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.ExternalApi.StackOverflow.Dtos;

public record AnswersResponseDto(
    [property: JsonPropertyName("items")] List<AnswerDto> Answers,
    [property: JsonPropertyName("has_more")] bool HasMore
    );
