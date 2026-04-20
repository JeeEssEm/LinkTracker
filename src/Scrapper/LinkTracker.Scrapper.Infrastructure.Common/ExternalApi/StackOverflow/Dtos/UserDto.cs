using System.Text.Json.Serialization;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.ExternalApi.StackOverflow.Dtos;

public record UserDto(
    [property: JsonPropertyName("display_name")] string Name
    );
