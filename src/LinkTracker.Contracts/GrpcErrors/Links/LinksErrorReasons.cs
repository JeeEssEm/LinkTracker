namespace LinkTracker.Contracts.GrpcErrors.Links;

public static class LinksErrorReasons
{
    public const string LinkNotFound = nameof(LinkNotFound);
    public const string LinkAlreadyExists = nameof(LinkAlreadyExists);
    public const string IncorrectUrlError = nameof(IncorrectUrlError);
}
