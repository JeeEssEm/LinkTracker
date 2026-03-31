using Google.Rpc;
using LinkTracker.Contracts.GrpcErrors;
using LinkTracker.Contracts.GrpcErrors.Chats;
using LinkTracker.Scrapper.ApiClients.Shared.Errors;
using LinkTracker.Scrapper.Features.Shared.Errors.Chat;

namespace LinkTracker.Scrapper.Features.Chats;

public class ChatsGrpcErrorsProfile : GrpcErrorProfile
{
    protected override string ErrorDomain => "Scrapper";

    public ChatsGrpcErrorsProfile()
    {
        CreateMapToServer<ChatNotFoundError>(code: Code.NotFound, reason: ChatErrorReasons.ChatNotFound);
        CreateMapToServer<ChatAlreadyExistsError>(Code.AlreadyExists, ChatErrorReasons.ChatAlreadyExists);
    }
}