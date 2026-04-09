using GrpcNamespace = LinkTracker.Bot.Grpc.Links;

namespace LinkTracker.Bot.App.Features.Api.Updates;

public static class UpdatesMapper
{
    public static LinkUpdatesRequest ToRequest(this GrpcNamespace.LinkUpdatesRequest request) => new(
        request.Id,
        request.Url,
        request.Description,
        request.ChatIds.ToList()
    );
}
