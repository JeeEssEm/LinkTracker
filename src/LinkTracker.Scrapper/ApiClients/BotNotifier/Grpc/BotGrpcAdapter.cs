using FluentResults;
using Grpc.Core;
using LinkTracker.Bot.App.Features.Api.Grpc;
using LinkTracker.Contracts.GrpcErrors;

namespace LinkTracker.Scrapper.ApiClients.BotNotifier.Grpc;

public class BotGrpcAdapter(BotService.BotServiceClient botClient, GrpcErrorMapper errorMapper) : IBotClient
{
    public async Task<Result> SendUpdateAsync(LinkUpdateRequest request)
    {
        try
        {
            await botClient.UpdateAsync(new LinkUpdatesRequest { ChatIds = { request.TgChatIds }, Id = request.Id, Description = request.Description, Url = request.Url });
            return Result.Ok();
        }
        catch (RpcException exception)
        {
            return Result.Fail(errorMapper.MapToDomainError(exception));
        }
    }
}