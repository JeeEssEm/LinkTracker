using LinkTracker.Scrapper.Features.Shared.Dtos;

namespace LinkTracker.Scrapper.Features.Links.GetLinks;

public record ListLinksResponse(List<LinkResponse> Links, int Size);
