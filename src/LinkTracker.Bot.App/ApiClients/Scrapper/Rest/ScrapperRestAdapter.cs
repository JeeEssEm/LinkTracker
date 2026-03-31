using System.Net;
using FluentResults;
using LinkTracker.Bot.App.ApiClients.Scrapper.Dtos;
using LinkTracker.Bot.App.ApiClients.Scrapper.Errors;
using LinkTracker.Bot.App.ApiClients.Scrapper.Errors.Chats;
using LinkTracker.Bot.App.ApiClients.Scrapper.Errors.Links;

namespace LinkTracker.Bot.App.ApiClients.Scrapper.Rest;

public class ScrapperRestAdapter(IScrapperRefitClient scrapperRefitClient) : IScrapperClient
{
    public async Task<Result> AddLinkAsync(AddLinkDto dto, long chatId)
    {
        var response = await scrapperRefitClient.AddLinkAsync(dto, chatId);
        // в задании указано, что надо строго следовать указанному OpenAPI.
        // В нём нет возможности управлять типами ссылок, поэтому пришлось оставить так
        var supportedLinkTypes = new List<string>
        {
            "https://stackoverflow.com/questions/question_id",
            "https://github.com/user/repo"
        };
        return response.StatusCode switch
        {
            HttpStatusCode.OK => Result.Ok(),
            HttpStatusCode.Conflict => Result.Fail(new LinkAlreadyExists()),
            HttpStatusCode.BadRequest => Result.Fail(new UnsupportedLinkType(supportedLinkTypes)),
            HttpStatusCode.NotFound => Result.Fail(new ChatNotFound()),
            _ => Result.Fail(new InternalError())
        };
    }

    public async Task<Result> RemoveLinkAsync(RemoveLinkDto dto, long chatId)
    {
        var response = await scrapperRefitClient.RemoveLinkAsync(dto, chatId);

        return response.StatusCode switch
        {
            HttpStatusCode.OK => Result.Ok(),
            HttpStatusCode.BadRequest => Result.Fail(new LinkNotFound()),
            HttpStatusCode.NotFound => Result.Fail(new ChatNotFound()),
            _ => Result.Fail(new InternalError())
        };
    }

    public async Task<Result<ListLinksResponse>> ListLinksAsync(long chatId)
    {
        var response = await scrapperRefitClient.ListLinksAsync(chatId);
        return response.StatusCode switch
        {
            HttpStatusCode.OK => Result.Ok(response.Content),
            HttpStatusCode.NotFound => Result.Fail(new ChatNotFound()),
            HttpStatusCode.BadRequest => Result.Fail(new IncorrectRequestParametersError()),
            _ => Result.Fail(new InternalError())
        };
    }

    public async Task<Result> AddTgChatAsync(long id)
    {
        var response = await scrapperRefitClient.AddTgChatAsync(id);
        return response.StatusCode switch
        {
            HttpStatusCode.OK => Result.Ok(),
            HttpStatusCode.Conflict => Result.Fail(new ChatAlreadyExists()),
            _ => Result.Fail(new InternalError())
        };
    }
}