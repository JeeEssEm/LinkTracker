namespace LinkTracker.Scrapper.Domain;

public record Link(long Id, string Url, string CheckerKey, List<string> Tags, List<string> Filters, Chat Owner, DateTimeOffset? LastCheckedAt = null);
