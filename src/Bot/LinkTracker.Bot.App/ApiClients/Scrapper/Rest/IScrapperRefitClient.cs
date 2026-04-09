using LinkTracker.Scrapper.Contracts.Dtos.Links;
using Refit;

namespace LinkTracker.Bot.App.ApiClients.Scrapper.Rest;

public interface IScrapperRefitClient
{
    [Post("/links")]
    Task<HttpResponseMessage> AddLinkAsync([Body] AddLinkDto dto, [Header("Tg-Chat-Id")] long chatId);

    [Delete("/links")]
    Task<HttpResponseMessage> RemoveLinkAsync([Body] RemoveLinkDto dto, [Header("Tg-Chat-Id")] long chatId);

    [Get("/links")]
    Task<ApiResponse<ListLinksResponse>> ListLinksAsync([Header("Tg-Chat-Id")] long chatId);

    [Post("/tg-chat/{id}")]
    Task<HttpResponseMessage> AddTgChatAsync(long id);
}

