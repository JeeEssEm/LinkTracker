using FluentResults;
using Grpc.Core;
using LinkTracker.Contracts.GrpcErrors;
using LinkTracker.Scrapper.Contracts.Dtos.Links;
using LinkTracker.Scrapper.Grpc.Chats;
using LinkTracker.Scrapper.Grpc.Links;
using AddLinkRequest = LinkTracker.Scrapper.Grpc.Links.AddLinkRequest;
using GrpcLinkResponse = LinkTracker.Scrapper.Grpc.Links.LinkResponse;
using LinkResponse = LinkTracker.Scrapper.Contracts.Dtos.Links.LinkResponse;
using ListLinksResponse = LinkTracker.Scrapper.Contracts.Dtos.Links.ListLinksResponse;
using RemoveLinkRequest = LinkTracker.Scrapper.Grpc.Links.RemoveLinkRequest;
using ScrapperChatsServiceClient = LinkTracker.Scrapper.Grpc.Chats.ScrapperChatsService.ScrapperChatsServiceClient;
using ScrapperLinksServiceClient = LinkTracker.Scrapper.Grpc.Links.ScrapperLinksService.ScrapperLinksServiceClient;

namespace LinkTracker.Bot.App.ApiClients.Scrapper.Grpc;

public class ScrapperGrpcAdapter(ScrapperChatsServiceClient chatsClient, ScrapperLinksServiceClient linksClient, GrpcErrorMapper errorMapper) : IScrapperClient
{
    public async Task<Result> AddLinkAsync(AddLinkDto dto, long chatId)
    {
        try
        {
            await linksClient.AddLinkAsync(new AddLinkRequest { ChatId = chatId, Link = dto.Link, Tags = { dto.Tags }, Filters = { dto.Filters } });
            return Result.Ok();
        }
        catch (RpcException exception)
        {
            return Result.Fail(errorMapper.MapToDomainError(exception));
        }
    }

    public async Task<Result> RemoveLinkAsync(RemoveLinkDto dto, long chatId)
    {
        try
        {
            await linksClient.RemoveLinkAsync(new RemoveLinkRequest { ChatId = chatId, Link = dto.Link });
            return Result.Ok();
        }
        catch (RpcException exception)
        {
            return Result.Fail(errorMapper.MapToDomainError(exception));
        }
    }

    public async Task<Result<ListLinksResponse>> ListLinksAsync(long chatId)
    {
        try
        {
            var resp = await linksClient.ListLinksAsync(new ListLinksRequest { ChatId = chatId });
            return Result.Ok(new ListLinksResponse(
                resp.Links.Select(lnk => new LinkResponse(Guid.Parse(lnk.Id), lnk.Url, lnk.Tags.ToList())).ToList(),
                resp.Links.Count
            ));
        }
        catch (RpcException exception)
        {
            return Result.Fail(errorMapper.MapToDomainError(exception));
        }
    }

    public async Task<Result> AddTgChatAsync(long id)
    {
        try
        {
            await chatsClient.CreateChatAsync(new CreateChatRequest { Id = id });
            return Result.Ok();
        }
        catch (RpcException exception)
        {
            return Result.Fail(errorMapper.MapToDomainError(exception));
        }
    }
}