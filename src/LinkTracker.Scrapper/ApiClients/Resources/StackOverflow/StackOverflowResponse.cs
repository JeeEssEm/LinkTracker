using System.Text.Json.Serialization;

namespace LinkTracker.Scrapper.ApiClients.Resources.StackOverflow;

public record StackOverflowResponse(List<StackOverflowQuestion> Items);

public record StackOverflowQuestion(
    [property: JsonPropertyName("question_id")] long QuestionId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("last_activity_date")] long LastActivityDate,
    [property: JsonPropertyName("answer_count")] int AnswerCount);
