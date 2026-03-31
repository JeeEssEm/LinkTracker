namespace LinkTracker.Bot.App.Features.Api.Updates;

public record LinkUpdatesRequest(long Id, string Url, string Description, List<long> TgChatIds);
