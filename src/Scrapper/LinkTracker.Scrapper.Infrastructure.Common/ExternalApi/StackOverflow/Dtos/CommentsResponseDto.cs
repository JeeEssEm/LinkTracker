using System.Text.Json.Serialization;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.ExternalApi.StackOverflow.Dtos;

public record CommentsResponseDto(
    [property: JsonPropertyName("items")] List<CommentDto> Comments,
    [property: JsonPropertyName("has_more")] bool HasMore
);
