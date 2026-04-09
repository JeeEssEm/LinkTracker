using FluentResults;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Contracts.Dtos.Links;
using LinkTracker.Scrapper.Errors.Chats;
using LinkTracker.Scrapper.Features.Shared;

namespace LinkTracker.Scrapper.Features.Links.GetLinks;

public class GetLinksHandler(ISubscriptionRepository subscriptionRepository, IChatRepository chatRepository)
{
    public async Task<Result<ListLinksResponse>> HandleAsync(long chatId)
    {
        var chat = await chatRepository.GetChatAsync(chatId);
        if (chat == null)
        {
            return Result.Fail(new ChatNotFound());
        }

        var links = await subscriptionRepository.GetSubscriptionsByChatAsync(chatId);

        return Result.Ok(new ListLinksResponse(
            links.Select(l => l.ToLinkResponse()).ToList(),
            links.Count
        ));
    }
}