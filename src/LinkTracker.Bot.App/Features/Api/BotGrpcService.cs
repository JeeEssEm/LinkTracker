using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LinkTracker.Bot.App.Features.Api.Grpc;
using LinkTracker.Bot.App.Features.Api.Shared.Errors;
using LinkTracker.Bot.App.Features.Api.Updates;
using LinkUpdatesRequestGrpc = LinkTracker.Bot.App.Features.Api.Grpc.LinkUpdatesRequest;

namespace LinkTracker.Bot.App.Features.Api;


class BotGrpcService(LinkUpdatesHandler handler) : BotService.BotServiceBase
{
    public override async Task<Empty> Update(LinkUpdatesRequestGrpc request, ServerCallContext context)
    {
        var result = await handler.HandleAsync(request.ToRequest());
        return result.ToGrpc();
    }
}
