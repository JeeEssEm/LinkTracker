using LinkTracker.Contracts.Rest;
using LinkTracker.Scrapper.Features.Shared;
using LinkTracker.Scrapper.Features.Shared.Errors;

namespace LinkTracker.Scrapper.Features.Chats.AddChat;

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
        .Produces<ApiErrorResponse>(409);
    }
}