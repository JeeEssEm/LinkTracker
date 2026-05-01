namespace LinkTracker.Scrapper.Contracts.Dtos.Links;

public record ListLinksResponse(IEnumerable<LinkResponse> Links, int Size);
