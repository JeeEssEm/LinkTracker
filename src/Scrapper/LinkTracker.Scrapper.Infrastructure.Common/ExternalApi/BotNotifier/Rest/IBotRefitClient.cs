using LinkTracker.Bot.Contracts.Rest;
using Refit;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.ExternalApi.BotNotifier.Rest;

public interface IBotRefitClient
{
    [Post("/updates")]
    Task<HttpResponseMessage> SendUpdateAsync([Body] LinkUpdateRequest request);
}
