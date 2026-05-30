namespace LinkTracker.Bot.Contracts.Rest;

public record LinkUpdateRequest(Guid Id, string Description, IReadOnlyList<long> TgChatIds, string Priority);
