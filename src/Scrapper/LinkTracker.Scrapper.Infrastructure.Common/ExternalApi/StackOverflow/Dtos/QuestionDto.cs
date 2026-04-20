using System.Text.Json.Serialization;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.ExternalApi.StackOverflow.Dtos;

public record QuestionDto(
    [property: JsonPropertyName("question_id")] long QuestionId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("creation_date")] long CreationDate,
    [property: JsonPropertyName("owner")] UserDto User
);
