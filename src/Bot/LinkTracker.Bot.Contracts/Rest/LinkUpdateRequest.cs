namespace LinkTracker.Bot.Contracts.Rest;

public record LinkUpdateRequest(long Id, string Url, string Description, IReadOnlyList<long> TgChatIds);
