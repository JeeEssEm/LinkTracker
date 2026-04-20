using System.Text.Json.Serialization;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.ExternalApi.Github.Dtos;

public record UserDto(
    [property: JsonPropertyName("login")] string Login,
    [property: JsonPropertyName("id")] long Id
);