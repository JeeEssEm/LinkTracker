namespace LinkTracker.Contracts.GrpcErrors.Chats;

public static class ChatErrorReasons
{
    public const string ChatNotFound = nameof(ChatNotFound);
    public const string ChatAlreadyExists = nameof(ChatAlreadyExists);
    public const string FailedToNotifyUsers = nameof(FailedToNotifyUsers);
}
