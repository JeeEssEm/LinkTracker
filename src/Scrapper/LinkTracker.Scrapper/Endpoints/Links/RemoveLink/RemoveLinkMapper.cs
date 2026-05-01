using LinkTracker.Scrapper.Contracts.Dtos.Links;
using GrpcNameSpace = LinkTracker.Scrapper.Grpc.Links;

namespace LinkTracker.Scrapper.Endpoints.Links.RemoveLink;

public static class RemoveLinkMapper
{
    public static RemoveLinkRequest ToRequest(this GrpcNameSpace.RemoveLinkRequest req) => new(req.Link);
}