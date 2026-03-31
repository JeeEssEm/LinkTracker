using FluentResults;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LinkTracker.Contracts.GrpcErrors;

namespace LinkTracker.Scrapper.Features.Shared.Errors;

public static class GrpcResultsExtensions
{
    public static TResponse ToGrpc<TValue, TResponse>(this Result<TValue> result, Func<TValue, TResponse> mapSuccess, GrpcErrorMapper mapper)
    {
        if (result.IsSuccess)
        {
            return mapSuccess(result.Value);
        }

        var error = result.Errors.First();
        var status = mapper.MapToRpcStatus(error);
        throw status.ToRpcException();
    }

    public static Empty ToGrpc(this Result result, GrpcErrorMapper mapper)
    {
        if (result.IsSuccess)
        {
            return new Empty();
        }

        var error = result.Errors.First();
        var status = mapper.MapToRpcStatus(error);
        throw status.ToRpcException();
    }
}