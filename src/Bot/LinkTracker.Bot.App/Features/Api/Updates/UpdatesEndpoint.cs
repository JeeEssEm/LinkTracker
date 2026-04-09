using LinkTracker.Bot.App.Features.Api.Shared.ResultExtensions;
using LinkTracker.Contracts.Rest;

namespace LinkTracker.Bot.App.Features.Api.Updates;

public static class UpdatesEndpoint
{
    public static void MapUpdates(this IEndpointRouteBuilder app)
    {
        app.MapPost("/updates", async (LinkUpdatesRequest request, LinkUpdatesHandler handler) =>
        {
            var result = await handler.HandleAsync(request);
            return result.ToMinimalApiResult();
        })
        .Produces(200)
        .Produces<ApiErrorResponse>(400);
    }
}
