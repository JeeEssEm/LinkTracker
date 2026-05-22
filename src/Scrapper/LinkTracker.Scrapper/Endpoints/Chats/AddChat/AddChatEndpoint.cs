using LinkTracker.Contracts.Rest;
using LinkTracker.Scrapper.Application.Handlers.Chats;
using LinkTracker.Scrapper.Endpoints.Shared;
using LinkTracker.Scrapper.Extensions;

namespace LinkTracker.Scrapper.Endpoints.Chats.AddChat;

public static class AddChatEndpoint
{
    public static void MapAddChat(this IEndpointRouteBuilder app)
    {
        app.MapPost("/tg-chat/{id:long}", async (AddChatHandler handler, long id) =>
        {
            var result = await handler.HandleAsync(id);
            return result.ToMinimalApiResult();
        })
        .Produces(200)
        .Produces<ApiErrorResponse>(400)
        .Produces<ApiErrorResponse>(409)
        .RequireRateLimiting(RateLimitPolitics.PerIpLimit);
    }
}