namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;

public record NotificationMessage(IReadOnlyList<long> UserIdsToNotify, string Message, string Url, long LinkId);
