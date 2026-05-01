using LinkTracker.Scrapper.Contracts.Dtos.Links;
using GrpcNameSpace = LinkTracker.Scrapper.Grpc.Links;
namespace LinkTracker.Scrapper.Endpoints.Links.AddLink;

public static class AddLinkMapper
{
    public static AddLinkRequest ToRequest(this GrpcNameSpace.AddLinkRequest req)
        => new AddLinkRequest(req.Link, req.Tags.ToList(), req.Filters.ToList());
}
