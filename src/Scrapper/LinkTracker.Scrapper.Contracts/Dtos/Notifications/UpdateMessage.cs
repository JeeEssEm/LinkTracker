namespace LinkTracker.Scrapper.Contracts.Dtos.Notifications;

public record UpdateMessage(Guid Id, string Url, string Author, string Description, IReadOnlyList<long> TgChatIds);
