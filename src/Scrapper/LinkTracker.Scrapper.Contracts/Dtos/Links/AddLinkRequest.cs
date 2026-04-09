namespace LinkTracker.Scrapper.Contracts.Dtos.Links;

public record AddLinkRequest(string Link, List<string> Tags, List<string> Filters);
