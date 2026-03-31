namespace LinkTracker.Bot.App.ApiClients.Scrapper.Dtos;

public record ListLinksResponse(List<LinkDto> Links, int Size);
public record LinkDto(long Id, string Url, List<string> Tags, List<string> Filters);
