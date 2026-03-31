using LinkTracker.Bot.App.Features.Api.Grpc;
using LinkUpdatesRequestGrpc = LinkTracker.Bot.App.Features.Api.Grpc.LinkUpdatesRequest;

namespace LinkTracker.Bot.App.Features.Api.Updates;

public static class UpdatesMapper
{
    public static LinkUpdatesRequest ToRequest(this LinkUpdatesRequestGrpc request) => new(
        request.Id,
        request.Url,
        request.Description,
        request.ChatIds.ToList()
    );
}
