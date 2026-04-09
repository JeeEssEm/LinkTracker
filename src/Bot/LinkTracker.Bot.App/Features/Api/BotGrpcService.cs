using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LinkTracker.Bot.App.Features.Api.Updates;
using LinkTracker.Bot.Grpc.Links;
using LinkTracker.Contracts.GrpcErrors;
using GrpcNamespace = LinkTracker.Bot.Grpc.Links;

namespace LinkTracker.Bot.App.Features.Api;

class BotGrpcService(LinkUpdatesHandler handler, GrpcErrorMapper mapper) : BotService.BotServiceBase
{
    public override async Task<Empty> Update(GrpcNamespace.LinkUpdatesRequest request, ServerCallContext context)
    {
        var result = await handler.HandleAsync(request.ToRequest());
        return result.ToGrpc(mapper);
    }
}
