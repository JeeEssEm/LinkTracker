using Google.Rpc;
using LinkTracker.Contracts.GrpcErrors;
using LinkTracker.Scrapper.Contracts.ErrorReasons;
using LinkTracker.Scrapper.Errors.Chats;

namespace LinkTracker.Scrapper.Contracts.GrpcErrorProfiles;

public class ChatsGrpcErrorsProfile : GrpcErrorProfile
{
    protected override string ErrorDomain => "Scrapper";

    public ChatsGrpcErrorsProfile()
    {
        CreateServerMappings();
        CreateClientMappings();
    }

    private void CreateServerMappings()
    {
        CreateMapToServer<ChatNotFound>(code: Code.NotFound, reason: ChatErrorReasons.ChatNotFound);
        CreateMapToServer<ChatAlreadyExists>(Code.AlreadyExists, ChatErrorReasons.ChatAlreadyExists);
    }

    private void CreateClientMappings()
    {
        CreateMapToClient<ChatNotFound>(ChatErrorReasons.ChatNotFound);
        CreateMapToClient<ChatAlreadyExists>(ChatErrorReasons.ChatAlreadyExists);
    }
}