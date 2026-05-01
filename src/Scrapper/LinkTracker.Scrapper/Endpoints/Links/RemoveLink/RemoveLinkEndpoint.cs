using LinkTracker.Contracts.Rest;
using LinkTracker.Scrapper.Application.Handlers.Links;
using LinkTracker.Scrapper.Contracts.Dtos.Links;
using LinkTracker.Scrapper.Endpoints.Shared;
using Microsoft.AspNetCore.Mvc;

namespace LinkTracker.Scrapper.Endpoints.Links.RemoveLink;

public static class RemoveLinkEndpoint
{
    public static void MapRemoveLink(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/links", async (
            [FromHeader(Name = "Tg-Chat-Id")] long tgChatId,
            [FromBody] RemoveLinkRequest request,
            RemoveLinkHandler handler) =>
        {
            var result = await handler.HandleAsync(request, tgChatId);
            return result.ToMinimalApiResult();
        })
        .Produces<LinkResponse>(200)
        .Produces<ApiErrorResponse>(400)
        .Produces<ApiErrorResponse>(404);
    }
}
