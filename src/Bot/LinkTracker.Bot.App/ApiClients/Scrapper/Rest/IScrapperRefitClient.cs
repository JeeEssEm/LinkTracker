using LinkTracker.Scrapper.Contracts.Dtos.Links;
using Refit;

namespace LinkTracker.Bot.App.ApiClients.Scrapper.Rest;

public interface IScrapperRefitClient
{
    [Post("/links")]
    [Headers("X-Source:Bot")]
    Task<HttpResponseMessage> AddLinkAsync([Body] AddLinkDto dto, [Header("Tg-Chat-Id")] long chatId);

    [Delete("/links")]
    [Headers("X-Source:Bot")]
    Task<HttpResponseMessage> RemoveLinkAsync([Body] RemoveLinkDto dto, [Header("Tg-Chat-Id")] long chatId);

    [Get("/links")]
    [Headers("X-Source:Bot")]
    Task<ApiResponse<ListLinksResponse>> ListLinksAsync([Header("Tg-Chat-Id")] long chatId);

    [Post("/tg-chat/{id}")]
    [Headers("X-Source:Bot")]
    Task<HttpResponseMessage> AddTgChatAsync(long id);
}

