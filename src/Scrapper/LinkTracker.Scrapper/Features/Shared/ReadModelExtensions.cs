using LinkTracker.Scrapper.Application.ReadModels;
using LinkTracker.Scrapper.Contracts.Dtos.Links;

namespace LinkTracker.Scrapper.Features.Shared;

public static class ReadModelExtensions
{
    public static LinkResponse ToLinkResponse(this SubscribedLink link) => new(link.Id, link.Url, link.Tags);
}