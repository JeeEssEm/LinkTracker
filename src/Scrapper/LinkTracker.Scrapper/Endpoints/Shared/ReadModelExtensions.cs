using LinkTracker.Scrapper.Application.ReadModels;
using LinkTracker.Scrapper.Contracts.Dtos.Links;

namespace LinkTracker.Scrapper.Endpoints.Shared;

public static class ReadModelExtensions
{
    public static LinkResponse ToLinkResponse(this SubscriptionDto link) => new(link.Id, link.Url, link.Tags);
}