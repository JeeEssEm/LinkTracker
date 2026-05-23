namespace LinkTracker.Bot.Contracts.Rest;

public record LinkUpdateRequest(Guid Id, string Url, string Description, IReadOnlyList<long> TgChatIds, string Priority);
