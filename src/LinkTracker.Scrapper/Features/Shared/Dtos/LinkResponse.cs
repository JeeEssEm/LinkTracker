using LinkTracker.Scrapper.Domain;

namespace LinkTracker.Scrapper.Features.Shared.Dtos;

public record LinkResponse(long Id, string Url, List<string> Tags, List<string> Filters)
{
    public static LinkResponse FromDomain(Link link) =>
        new(link.Id, link.Url, link.Tags, link.Filters);
}
