using FluentResults;
using Google.Protobuf.WellKnownTypes;
using Google.Rpc;
using Type = System.Type;

namespace LinkTracker.Contracts.GrpcErrors;


public abstract class GrpcErrorProfile
{
    protected virtual string ErrorDomain => "unknown";

    internal Dictionary<Type, Func<IError, Status>> ServerMappings { get; } = new();
    internal Dictionary<string, Func<ErrorInfo, Status, IError>> ClientMappings { get; } = new();

    protected void CreateMapToServer<TError>(Func<TError, Status> map) where TError : IError
    {
        ServerMappings[typeof(TError)] = error => map((TError)error);
    }

    protected void CreateMapToServer<TError>(Code code, string reason) where TError : IError
    {
        CreateMapToServer<TError>(error => CreateStatus(code, error.Message, reason));
    }

    protected void CreateMapToServer<TError>(Code code, string reason, Func<TError, Dictionary<string, string>> getMetadata) where TError : IError
    {
        CreateMapToServer<TError>(error =>
        {
            var status = CreateStatus(code, error.Message, reason);
            var errorInfo = status.GetDetail<ErrorInfo>();

            foreach (var kvp in getMetadata(error))
            {
                errorInfo.Metadata.Add(kvp.Key, kvp.Value);
            }

            UpdateDetail(status, errorInfo);
            return status;
        });
    }

    protected void CreateMapToServer<TError>(Code code, string reason, string resourceType, Func<TError, string> getResourceName) where TError : IError
    {
        CreateMapToServer<TError>(error =>
        {
            var status = CreateStatus(code, error.Message, reason);
            var resourceInfo = new ResourceInfo { ResourceType = resourceType, ResourceName = getResourceName(error) };

            status.Details.Add(Any.Pack(resourceInfo));
            return status;
        });
    }

    protected void CreateMapToServer<TError>(Code code, string reason, string resourceType, Func<TError, string> getResourceName, Func<TError, Dictionary<string, string>> getMetadata) where TError : IError
    {
        CreateMapToServer<TError>(error =>
        {
            var status = CreateStatus(code, error.Message, reason);

            var errorInfo = status.GetDetail<ErrorInfo>();
            foreach (var kvp in getMetadata(error))
            {
                errorInfo.Metadata.Add(kvp.Key, kvp.Value);
            }

            UpdateDetail(status, errorInfo);

            var resourceInfo = new ResourceInfo { ResourceType = resourceType, ResourceName = getResourceName(error) };
            status.Details.Add(Any.Pack(resourceInfo));

            return status;
        });
    }

    protected void CreateMapToClient(string reason, Func<ErrorInfo, Status, IError> map)
    {
        ClientMappings[reason] = map;
    }

    protected void CreateMapToClient<TError>(string reason) where TError : IError, new()
    {
        CreateMapToClient(reason, (_, _) => new TError());
    }

    protected void CreateMapToClient(string reason, Func<string, IError> mapFromResourceName)
    {
        CreateMapToClient(reason, (_, status) =>
        {
            var resourceInfo = status.GetDetail<ResourceInfo>();
            return mapFromResourceName(resourceInfo?.ResourceName ?? string.Empty);
        });
    }

    protected void CreateMapToClient(string reason, Func<IDictionary<string, string>, IError> mapFromMetadata)
    {
        CreateMapToClient(reason, (errorInfo, _) => mapFromMetadata(errorInfo.Metadata));
    }

    private Status CreateStatus(Code code, string message, string reason)
    {
        var status = new Status { Code = (int)code, Message = message };
        var errorInfo = new ErrorInfo { Reason = reason, Domain = ErrorDomain };
        status.Details.Add(Any.Pack(errorInfo));
        return status;
    }

    private void UpdateDetail<T>(Status status, T message) where T : Google.Protobuf.IMessage<T>
    {
        var any = status.Details.First(d => d.Is(message.Descriptor));
        status.Details.Remove(any);
        status.Details.Add(Any.Pack(message));
    }
}