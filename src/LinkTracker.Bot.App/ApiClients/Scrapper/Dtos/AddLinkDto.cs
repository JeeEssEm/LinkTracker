namespace LinkTracker.Bot.App.ApiClients.Scrapper.Dtos;

public record AddLinkDto(string Link, List<string> Tags, List<string> Filters);
