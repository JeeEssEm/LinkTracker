namespace LinkTracker.Scrapper.Contracts.Dtos.Links;

public record AddLinkDto(string Link, List<string> Tags, List<string> Filters);
