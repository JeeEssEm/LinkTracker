using Grpc.Core;
using LinkTracker.Contracts.GrpcErrors;
using LinkTracker.Scrapper.Features.Links.AddLink;
using LinkTracker.Scrapper.Features.Links.GetLinks;
using LinkTracker.Scrapper.Features.Links.RemoveLink;
using LinkTracker.Scrapper.Features.Shared.Dtos;
using LinkTracker.Scrapper.Features.Shared.Errors;

namespace LinkTracker.Scrapper.Features.Links;

public class LinksGrpcService(AddLinkHandler addLinkHandler, RemoveLinkHandler removeLinkHandler, GetLinksHandler getLinksHandler, GrpcErrorMapper mapper) : Grpc.ScrapperLinksService.ScrapperLinksServiceBase
{
    public override async Task<Grpc.LinkResponse> AddLink(Grpc.AddLinkRequest request, ServerCallContext context)
    {
        var result = await addLinkHandler.HandleAsync(request.ToRequest(), request.ChatId);
        return result.ToGrpc(MapLinkResponse, mapper);
    }

    public override async Task<Grpc.LinkResponse> RemoveLink(Grpc.RemoveLinkRequest request, ServerCallContext context)
    {
        var result = await removeLinkHandler.HandleAsync(request.ToRequest(), request.ChatId);
        return result.ToGrpc(MapLinkResponse, mapper);
    }

    public override async Task<Grpc.ListLinksResponse> ListLinks(Grpc.ListLinksRequest request, ServerCallContext context)
    {
        var result = await getLinksHandler.HandleAsync(request.ChatId);
        return result.ToGrpc(handlerResponse =>
        {
            var resp = new Grpc.ListLinksResponse { Size = handlerResponse.Size };
            resp.Links.AddRange(handlerResponse.Links.Select(MapLinkResponse));
            return resp;
        }, mapper);
    }

    private static Grpc.LinkResponse MapLinkResponse(LinkResponse link)
    {
        var resp = new Grpc.LinkResponse { Id = link.Id, Url = link.Url };
        resp.Filters.AddRange(link.Filters);
        resp.Tags.AddRange(link.Tags);
        return resp;
    }
}