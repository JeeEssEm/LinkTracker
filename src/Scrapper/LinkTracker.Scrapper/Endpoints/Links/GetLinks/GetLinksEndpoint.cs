using LinkTracker.Contracts.Rest;
using LinkTracker.Scrapper.Application.Handlers.Links;
using LinkTracker.Scrapper.Contracts.Dtos.Links;
using LinkTracker.Scrapper.Endpoints.Shared;
using LinkTracker.Scrapper.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace LinkTracker.Scrapper.Endpoints.Links.GetLinks;

public static class GetLinksEndpoint
{
    public static void MapGetLinks(this IEndpointRouteBuilder app)
    {
        app.MapGet("/links", async (
            [FromHeader(Name = "Tg-Chat-Id")] long tgChatId,
            GetLinksHandler handler) =>
        {
            var result = await handler.HandleAsync(tgChatId);
            return result.ToMinimalApiResult();
        })
        .Produces<ListLinksResponse>(200)
        .Produces<ApiErrorResponse>(400)
        .Produces<ApiErrorResponse>(404)
        .RequireRateLimiting(RateLimitPolitics.PerIpLimit);
    }
}
