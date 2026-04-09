using Grpc.Core;
using LinkTracker.Contracts.GrpcErrors;
using LinkTracker.Scrapper.Features.Links.AddLink;
using LinkTracker.Scrapper.Features.Links.GetLinks;
using LinkTracker.Scrapper.Features.Links.RemoveLink;
using GrpcNameSpace = LinkTracker.Scrapper.Grpc.Links;
using LinkResponse = LinkTracker.Scrapper.Contracts.Dtos.Links.LinkResponse;
using ScrapperLinksServiceBase = LinkTracker.Scrapper.Grpc.Links.ScrapperLinksService.ScrapperLinksServiceBase;

namespace LinkTracker.Scrapper.Features.Links;

public class LinksGrpcService(AddLinkHandler addLinkHandler, RemoveLinkHandler removeLinkHandler, GetLinksHandler getLinksHandler, GrpcErrorMapper mapper) : ScrapperLinksServiceBase
{
    public override async Task<GrpcNameSpace.LinkResponse> AddLink(GrpcNameSpace.AddLinkRequest request, ServerCallContext context)
    {
        var result = await addLinkHandler.HandleAsync(request.ToRequest(), request.ChatId);
        return result.ToGrpc(MapLinkResponse, mapper);
    }

    public override async Task<GrpcNameSpace.LinkResponse> RemoveLink(GrpcNameSpace.RemoveLinkRequest request, ServerCallContext context)
    {
        var result = await removeLinkHandler.HandleAsync(request.ToRequest(), request.ChatId);
        return result.ToGrpc(MapLinkResponse, mapper);
    }

    public override async Task<GrpcNameSpace.ListLinksResponse> ListLinks(GrpcNameSpace.ListLinksRequest request, ServerCallContext context)
    {
        var result = await getLinksHandler.HandleAsync(request.ChatId);
        return result.ToGrpc(handlerResponse =>
        {
            var resp = new GrpcNameSpace.ListLinksResponse { Size = handlerResponse.Size };
            resp.Links.AddRange(handlerResponse.Links.Select(MapLinkResponse));
            return resp;
        }, mapper);
    }

    private static GrpcNameSpace.LinkResponse MapLinkResponse(LinkResponse link)
    {
        var resp = new GrpcNameSpace.LinkResponse { Id = link.Id, Url = link.Url };
        resp.Tags.AddRange(link.Tags);
        return resp;
    }
}