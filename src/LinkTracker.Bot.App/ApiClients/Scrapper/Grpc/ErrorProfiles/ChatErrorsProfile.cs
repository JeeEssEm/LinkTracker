using LinkTracker.Bot.App.ApiClients.Scrapper.Errors.Chats;
using LinkTracker.Contracts.GrpcErrors;
using LinkTracker.Contracts.GrpcErrors.Chats;

namespace LinkTracker.Bot.App.ApiClients.Scrapper.Grpc.ErrorProfiles;

public class ChatErrorsProfile : GrpcErrorProfile
{
    public ChatErrorsProfile()
    {
        CreateMapToClient<ChatNotFound>(ChatErrorReasons.ChatNotFound);
        CreateMapToClient<ChatAlreadyExists>(ChatErrorReasons.ChatAlreadyExists);
    }
}