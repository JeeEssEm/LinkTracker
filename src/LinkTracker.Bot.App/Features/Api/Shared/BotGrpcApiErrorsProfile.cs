using Google.Rpc;
using LinkTracker.Bot.App.Features.Api.Shared.Errors;
using LinkTracker.Contracts.GrpcErrors;
using LinkTracker.Contracts.GrpcErrors.Chats;

namespace LinkTracker.Bot.App.Features.Api.Shared;

public class BotGrpcApiErrorsProfile : GrpcErrorProfile
{
    public BotGrpcApiErrorsProfile()
    {
        CreateMapToServer<FailedToNotifyUsersError>(Code.Unknown, ChatErrorReasons.FailedToNotifyUsers, error => new Dictionary<string, string>()
        {
            {"chats", string.Join(", ", error.ChatIds)}
        });
    }
}