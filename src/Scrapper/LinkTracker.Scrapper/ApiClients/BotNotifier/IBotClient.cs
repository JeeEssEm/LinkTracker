using FluentResults;
using LinkTracker.Bot.Contracts.Rest;

namespace LinkTracker.Scrapper.ApiClients.BotNotifier;

public interface IBotClient
{
    Task<Result> SendUpdateAsync(LinkUpdateRequest request);
}
