using FluentResults;
using LinkTracker.Bot.Contracts.Rest;

namespace LinkTracker.Scrapper.Application.Scraping.ApiClients.Bot;

public interface IBotClient
{
    Task<Result> SendUpdateAsync(LinkUpdateRequest request);
}
