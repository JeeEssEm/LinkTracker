namespace LinkTracker.Scrapper.Contracts.Dtos.Links;

public record LinkResponse(long Id, string Url, IReadOnlyList<string> Tags);
