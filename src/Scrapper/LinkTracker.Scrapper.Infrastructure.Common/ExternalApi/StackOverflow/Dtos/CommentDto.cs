using System.Text.Json.Serialization;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.ExternalApi.StackOverflow.Dtos;

public record CommentDto(
    [property: JsonPropertyName("comment_id")] long CommentId,
    [property: JsonPropertyName("post_id")] long PostId,
    [property: JsonPropertyName("body")] string Body,
    [property: JsonPropertyName("creation_date")] long CreationDate,
    [property: JsonPropertyName("owner")] UserDto User
);
