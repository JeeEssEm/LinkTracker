using Google.Rpc;
using LinkTracker.Bot.Contracts.ErrorReasons;
using LinkTracker.Bot.Contracts.Errors;
using LinkTracker.Contracts.GrpcErrors;

namespace LinkTracker.Bot.Contracts.GrpcErrorProfiles;

public class UpdatesGrpcErrorsProfile : GrpcErrorProfile
{
    public UpdatesGrpcErrorsProfile()
    {
        CreateMapToServer<FailedToNotifyUsers>(Code.Unknown, UpdatesErrorReasons.FailedToNotifyUsers, error => new Dictionary<string, string>()
        {
            {"chats", string.Join(", ", error.ChatIds)}
        });
        CreateMapToClient(UpdatesErrorReasons.FailedToNotifyUsers, (error, metadata) =>
        {
            var details = metadata.GetDetail<ErrorInfo>();
            var chatIds = details.Metadata.GetValueOrDefault("chats", "")
                .Split(", ")
                .Select(long.Parse)
                .ToList();

            return new FailedToNotifyUsers(chatIds);
        });
    }
}