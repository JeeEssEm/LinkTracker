using FluentResults;
using Grpc.Core;
using LinkTracker.Bot.Contracts.Rest;
using LinkTracker.Bot.Grpc.Links;
using LinkTracker.Contracts.GrpcErrors;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Bot;
using BotServiceClient = LinkTracker.Bot.Grpc.Links.BotService.BotServiceClient;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.ExternalApi.BotNotifier.Grpc;

public class BotGrpcAdapter(BotServiceClient botClient, GrpcErrorMapper errorMapper) : IBotClient
{
    public async Task<Result> SendUpdateAsync(LinkUpdateRequest request)
    {
        // TODO: а че теперь делать с HTTP и gRPC клиентами, если все сообщения идут через AI Agent Service????
        try
        {
            await botClient.UpdateAsync(new LinkUpdatesRequest { ChatIds = { request.TgChatIds }, Id = request.Id.ToString(), Description = request.Description, Url = request.Url, Priority = "??" });
            return Result.Ok();
        }
        catch (RpcException exception)
        {
            return Result.Fail(errorMapper.MapToDomainError(exception));
        }
    }
}