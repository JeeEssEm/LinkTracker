namespace LinkTracker.Scrapper.Application.ReadModels;

public record SubscribedLink(long Id, string Url, IReadOnlyList<string> Tags);
