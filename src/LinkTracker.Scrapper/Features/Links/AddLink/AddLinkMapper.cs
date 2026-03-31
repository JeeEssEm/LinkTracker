using AddLinkRequestGrpc = LinkTracker.Scrapper.Features.Links.Grpc.AddLinkRequest;

namespace LinkTracker.Scrapper.Features.Links.AddLink;

public static class AddLinkMapper
{
    public static AddLinkRequest ToRequest(this AddLinkRequestGrpc req)
        => new AddLinkRequest(req.Link, req.Tags.ToList(), req.Filters.ToList());
}