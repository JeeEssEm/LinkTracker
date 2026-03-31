namespace LinkTracker.Scrapper.Features.Links.AddLink;

public record AddLinkRequest(string Link, List<string> Tags, List<string> Filters);
