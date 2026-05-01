using LinkTracker.Contracts.Rest;
using LinkTracker.Scrapper.Application.Handlers.Links;
using LinkTracker.Scrapper.Contracts.Dtos.Links;
using LinkTracker.Scrapper.Endpoints.Shared;
using Microsoft.AspNetCore.Mvc;

namespace LinkTracker.Scrapper.Endpoints.Links.AddLink;

public static class AddLinkEndpoint
{
    public static void MapAddLink(this IEndpointRouteBuilder app)
    {
        app.MapPost("/links", async (
            [FromHeader(Name = "Tg-Chat-Id")] long tgChatId,
            AddLinkRequest request,
            AddLinkHandler handler) =>
        {
            var result = await handler.HandleAsync(request, tgChatId);
            return result.ToMinimalApiResult();
        })
        .Produces<LinkResponse>(200)
        .Produces<ApiErrorResponse>(400)
        .Produces<ApiErrorResponse>(404)
        .Produces<ApiErrorResponse>(409);
    }
}