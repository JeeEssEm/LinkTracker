using RemoveLinkRequestGrpc = LinkTracker.Scrapper.Features.Links.Grpc.RemoveLinkRequest;
namespace LinkTracker.Scrapper.Features.Links.RemoveLink;

public static class RemoveLinkMapper
{
    public static RemoveLinkRequest ToRequest(this RemoveLinkRequestGrpc req) => new(req.Link);
}