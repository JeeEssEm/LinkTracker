using LinkTracker.Contracts.Rest;
using LinkTracker.Scrapper.Features.Shared;
using LinkTracker.Scrapper.Features.Shared.Errors;

namespace LinkTracker.Scrapper.Features.Chats.RemoveChat;

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
