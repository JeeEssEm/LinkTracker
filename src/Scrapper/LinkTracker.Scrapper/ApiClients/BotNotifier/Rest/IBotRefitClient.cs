using LinkTracker.Bot.Contracts.Rest;
using Refit;

namespace LinkTracker.Scrapper.ApiClients.BotNotifier.Rest;

public interface IBotRefitClient
{
    [Post("/updates")]
    Task<HttpResponseMessage> SendUpdateAsync([Body] LinkUpdateRequest request);
}
