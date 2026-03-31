using FluentResults;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace LinkTracker.Bot.App.Features.Api.Shared.Errors;

public static class GrpcResultsExtensions
{
    public static TResponse ToGrpc<TValue, TResponse>(this Result<TValue> result, Func<TValue, TResponse> mapSuccess)
    {
        if (result.IsSuccess)
        {
            return mapSuccess(result.Value);
        }
        throw result.ToRpcException();
    }

    public static Empty ToGrpc(this Result result)
    {
        if (result.IsSuccess)
        {
            return new Empty();
        }

        throw result.ToRpcException();
    }

    public static RpcException ToRpcException(this ResultBase result)
    {
        var error = result.Errors.FirstOrDefault();

        var statusCode = error switch
        {
            IncorrectRequestParametersError => StatusCode.InvalidArgument,
            _ => StatusCode.Internal
        };

        return new RpcException(new Status(statusCode, error?.Message ?? "Unknown error"));
    }
}