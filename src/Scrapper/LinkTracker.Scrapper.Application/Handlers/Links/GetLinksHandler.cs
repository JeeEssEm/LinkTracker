using FluentResults;
using LinkTracker.Scrapper.Application.QueryServices;
using LinkTracker.Scrapper.Contracts.Dtos.Links;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
using LinkTracker.Scrapper.Errors.Chats;

namespace LinkTracker.Scrapper.Application.Handlers.Links;

public class GetLinksHandler(ISubscriptionQueryService subQueries, IChatRepository chatRepository)
{
    public async Task<Result<ListLinksResponse>> HandleAsync(long chatId)
    {
        var id = new ChatId(chatId);
        if (!await chatRepository.ExistsAsync(id))
        {
            return Result.Fail(new ChatNotFound());
        }

        var links = await subQueries.ListSubscriptionsByChatAsync(id);
        return Result.Ok(new ListLinksResponse(links, links.Count));
    }
}
