using Google.Rpc;
using LinkTracker.Contracts.GrpcErrors;
using LinkTracker.Scrapper.Contracts.ErrorReasons;
using LinkTracker.Scrapper.Errors.Links;

namespace LinkTracker.Scrapper.Contracts.GrpcErrorProfiles;

public class LinksGrpcErrorsProfile : GrpcErrorProfile
{
    protected override string ErrorDomain => "Links";

    public LinksGrpcErrorsProfile()
    {
        CreateServerMappings();
        CreateClientMappings();
    }

    private void CreateServerMappings()
    {
        CreateMapToServer<LinkNotFound>(Code.NotFound, LinksErrorReasons.LinkNotFound);
        CreateMapToServer<LinkAlreadyExists>(Code.AlreadyExists, LinksErrorReasons.LinkAlreadyExists);
        CreateMapToServer<UnsupportedLinkType>(Code.InvalidArgument, LinksErrorReasons.IncorrectUrlError, error => new Dictionary<string, string> { { "supported", string.Join("\n", error.SupportedTypes) } });
    }

    private void CreateClientMappings()
    {
        CreateMapToClient<LinkNotFound>(LinksErrorReasons.LinkNotFound);
        CreateMapToClient<LinkAlreadyExists>(LinksErrorReasons.LinkAlreadyExists);
        CreateMapToClient(LinksErrorReasons.IncorrectUrlError, (error, metadata) =>
        {
            var details = metadata.GetDetail<ErrorInfo>();
            var supportedTypes = details.Metadata.GetValueOrDefault("supported", "").Split("\n");
            return new UnsupportedLinkType(supportedTypes.ToList());
        });
    }
}