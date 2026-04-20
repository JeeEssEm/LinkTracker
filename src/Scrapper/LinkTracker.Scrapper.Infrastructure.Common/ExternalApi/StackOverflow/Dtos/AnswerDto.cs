using System.Text.Json.Serialization;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.ExternalApi.StackOverflow.Dtos;

public record AnswerDto(
    [property: JsonPropertyName("question_id")] long QuestionId,
    [property: JsonPropertyName("answer_id")] long AnswerId,
    [property: JsonPropertyName("body")] string Body,
    [property: JsonPropertyName("last_activity_date")] long LastActivityDate,
    [property: JsonPropertyName("owner")] UserDto User
);
