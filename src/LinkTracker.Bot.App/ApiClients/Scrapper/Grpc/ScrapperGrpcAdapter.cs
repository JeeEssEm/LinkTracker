using FluentResults;
using Grpc.Core;
using LinkTracker.Bot.App.ApiClients.Scrapper.Dtos;
using LinkTracker.Bot.App.ApiClients.Scrapper.Errors;
using LinkTracker.Bot.App.ApiClients.Scrapper.Errors.Chats;
using LinkTracker.Bot.App.ApiClients.Scrapper.Errors.Links;
using LinkTracker.Contracts.GrpcErrors;
using LinkTracker.Scrapper.Features.Chats.Grpc;
using LinkTracker.Scrapper.Features.Links.Grpc;
using ListLinksResponse = LinkTracker.Bot.App.ApiClients.Scrapper.Dtos.ListLinksResponse;

namespace LinkTracker.Bot.App.ApiClients.Scrapper.Grpc;

public class ScrapperGrpcAdapter(ScrapperChatsService.ScrapperChatsServiceClient chatsClient, ScrapperLinksService.ScrapperLinksServiceClient linksClient, GrpcErrorMapper errorMapper) : IScrapperClient
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
                resp.Links.Select(lnk => new LinkDto(lnk.Id, lnk.Url, lnk.Tags.ToList(), lnk.Filters.ToList())).ToList(),
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