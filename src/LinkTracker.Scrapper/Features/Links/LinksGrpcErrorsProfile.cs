using Google.Rpc;
using LinkTracker.Contracts.GrpcErrors;
using LinkTracker.Contracts.GrpcErrors.Chats;
using LinkTracker.Contracts.GrpcErrors.Links;
using LinkTracker.Scrapper.ApiClients.Shared.Errors;
using LinkTracker.Scrapper.Features.Shared.Errors.Link;

namespace LinkTracker.Scrapper.Features.Links;

public class LinksGrpcErrorsProfile : GrpcErrorProfile
{
    protected override string ErrorDomain => "Links";

    public LinksGrpcErrorsProfile()
    {
        CreateMapToServer<LinkNotFoundError>(Code.NotFound, LinksErrorReasons.LinkNotFound);
        CreateMapToServer<LinkAlreadyExistsError>(Code.AlreadyExists, LinksErrorReasons.LinkAlreadyExists);
        CreateMapToServer<IncorrectUrlError>(Code.InvalidArgument, LinksErrorReasons.IncorrectUrlError, error => new Dictionary<string, string> {
            {"supported", string.Join("\n", error.SupportedTypes)}
        });
    }
}