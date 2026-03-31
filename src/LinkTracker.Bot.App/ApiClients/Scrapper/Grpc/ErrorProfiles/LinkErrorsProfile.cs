using Google.Rpc;
using LinkTracker.Bot.App.ApiClients.Scrapper.Errors.Links;
using LinkTracker.Contracts.GrpcErrors;
using LinkTracker.Contracts.GrpcErrors.Links;

namespace LinkTracker.Bot.App.ApiClients.Scrapper.Grpc.ErrorProfiles;

public class LinkErrorsProfile : GrpcErrorProfile
{
    public LinkErrorsProfile()
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