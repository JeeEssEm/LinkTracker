using FluentResults;
using Google.Rpc;
using Grpc.Core;
using Status = Google.Rpc.Status;

namespace LinkTracker.Contracts.GrpcErrors;

public class GrpcErrorMapper
{
    private readonly Dictionary<Type, Func<IError, Status>> _serverRegistry = new();
    private readonly Dictionary<string, Func<ErrorInfo, Status, IError>> _clientRegistry = new();

    public GrpcErrorMapper(IEnumerable<GrpcErrorProfile> profiles)
    {
        foreach (var profile in profiles)
        {
            foreach (var profileServerMapping in profile.ServerMappings)
            {
                _serverRegistry[profileServerMapping.Key] = profileServerMapping.Value;
            }

            foreach (var profileClientMapping in profile.ClientMappings)
            {
                _clientRegistry[profileClientMapping.Key] = profileClientMapping.Value;
            }
        }
    }

    public Status MapToRpcStatus(IError error)
    {
        if (_serverRegistry.TryGetValue(error.GetType(), out var map))
        {
            return map(error);
        }

        return new Status { Code = (int)Code.Internal, Message = error.Message };
    }

    public IError MapToDomainError(RpcException exception)
    {
        var status = exception.GetRpcStatus();
        if (status != null)
        {
            var errorInfo = status.GetDetail<ErrorInfo>();
            if (errorInfo != null && _clientRegistry.TryGetValue(errorInfo.Reason, out var map))
            {
                return map(errorInfo, status);
            }
        }

        return new Error(exception.Status.Detail);
    }
}