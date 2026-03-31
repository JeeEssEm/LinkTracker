using FluentResults;
using LinkTracker.Bot.App.ApiClients.Scrapper.Dtos;

namespace LinkTracker.Bot.App.ApiClients.Scrapper;

public interface IScrapperClient
{
    Task<Result> AddLinkAsync(AddLinkDto dto, long chatId);
    Task<Result> RemoveLinkAsync(RemoveLinkDto dto, long chatId);
    Task<Result<ListLinksResponse>> ListLinksAsync(long chatId);
    Task<Result> AddTgChatAsync(long id);
}