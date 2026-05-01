namespace LinkTracker.Scrapper.Contracts.Dtos.Links;

public record LinkResponse(Guid Id, string Url, IReadOnlyList<string> Tags);
