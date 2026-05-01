using LinkTracker.Contracts.Rest;
using LinkTracker.Scrapper.Application.Handlers.Chats;
using LinkTracker.Scrapper.Endpoints.Shared;

namespace LinkTracker.Scrapper.Endpoints.Chats.RemoveChat;

public static class RemoveChatEndpoint
{
    public static void MapRemoveChat(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/tg-chat/{id:long}", async (RemoveChatHandler handler, long id) =>
        {
            var result = await handler.HandleAsync(id);
            return result.ToMinimalApiResult();
        })
        .Produces(200)
        .Produces<ApiErrorResponse>(400)
        .Produces<ApiErrorResponse>(404);
    }
}
