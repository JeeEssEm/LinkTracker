namespace LinkTracker.Scrapper.Contracts.Dtos.Links;

public record LinkDto(long Id, string Url, List<string> Tags, List<string> Filters);
