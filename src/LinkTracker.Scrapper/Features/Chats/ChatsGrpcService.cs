using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LinkTracker.Contracts.GrpcErrors;
using LinkTracker.Scrapper.Features.Chats.AddChat;
using LinkTracker.Scrapper.Features.Chats.Grpc;
using LinkTracker.Scrapper.Features.Chats.RemoveChat;
using LinkTracker.Scrapper.Features.Shared.Errors;

namespace LinkTracker.Scrapper.Features.Chats;

public class ChatsGrpcService(AddChatHandler addChatHandler, RemoveChatHandler removeChatHandler, GrpcErrorMapper mapper) : ScrapperChatsService.ScrapperChatsServiceBase
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