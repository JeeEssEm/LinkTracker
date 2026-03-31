namespace LinkTracker.Scrapper.ApiClients.BotNotifier;

public record LinkUpdateRequest(long Id, string Url, string Description, List<long> TgChatIds);
