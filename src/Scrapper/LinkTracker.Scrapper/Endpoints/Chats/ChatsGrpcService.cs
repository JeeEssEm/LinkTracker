using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LinkTracker.Contracts.GrpcErrors;
using LinkTracker.Scrapper.Application.Handlers;
using LinkTracker.Scrapper.Application.Handlers.Chats;
using LinkTracker.Scrapper.Grpc.Chats;
using ScrapperChatsServiceBase = LinkTracker.Scrapper.Grpc.Chats.ScrapperChatsService.ScrapperChatsServiceBase;

namespace LinkTracker.Scrapper.Endpoints.Chats;

public class ChatsGrpcService(AddChatHandler addChatHandler, RemoveChatHandler removeChatHandler, GrpcErrorMapper mapper) : ScrapperChatsServiceBase
{
    public override async Task<Empty> CreateChat(CreateChatRequest request, ServerCallContext context)
    {
        var result = await addChatHandler.HandleAsync(request.Id);
        return result.ToGrpc(mapper);
    }

    public override async Task<Empty> RemoveChat(RemoveChatRequest request, ServerCallContext context)
    {
        var result = await removeChatHandler.HandleAsync(request.Id);
        return result.ToGrpc(mapper);
    }
}