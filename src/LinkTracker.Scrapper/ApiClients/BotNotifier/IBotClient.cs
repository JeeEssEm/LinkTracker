using FluentResults;

namespace LinkTracker.Scrapper.ApiClients.BotNotifier;

public interface IBotClient
{
    Task<Result> SendUpdateAsync(LinkUpdateRequest request);
}
