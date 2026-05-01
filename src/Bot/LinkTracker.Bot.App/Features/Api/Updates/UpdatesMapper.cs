using LinkTracker.Bot.Contracts.Rest;
using GrpcNamespace = LinkTracker.Bot.Grpc.Links;

namespace LinkTracker.Bot.App.Features.Api.Updates;

public static class UpdatesMapper
{
    public static LinkUpdateRequest ToRequest(this GrpcNamespace.LinkUpdatesRequest request) => new(
        Guid.Parse(request.Id),
        request.Url,
        request.Description,
        request.ChatIds
    );
}
