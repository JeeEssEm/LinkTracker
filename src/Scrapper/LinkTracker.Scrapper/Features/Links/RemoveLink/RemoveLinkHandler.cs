using FluentResults;
using LinkTracker.Scrapper.Application.Abstractions;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Contracts.Dtos.Links;
using LinkTracker.Scrapper.Errors.Chats;
using LinkTracker.Scrapper.Errors.Links;
using LinkTracker.Scrapper.Features.Shared;

namespace LinkTracker.Scrapper.Features.Links.RemoveLink;

public class RemoveLinkHandler(
    IChatRepository chatRepository,
    ISubscriptionRepository subscriptionRepository,
    ITagRepository tagRepository,
    IUnitOfWork unitOfWork
    )
{
    public async Task<Result<LinkResponse>> HandleAsync(RemoveLinkRequest request, long chatId)
    {
        await unitOfWork.BeginAsync();

        var chat = await chatRepository.GetChatAsync(chatId);
        if (chat == null)
        {
            return Result.Fail(new ChatNotFound());
        }

        var link = await subscriptionRepository.GetSubscriptionAsync(chatId, request.Link);
        if (link == null)
        {
            return Result.Fail(new LinkNotFound());
        }

        await subscriptionRepository.RemoveSubscriptionAsync(chatId, request.Link);
        await tagRepository.RemoveConnectionsTagsToLink(chatId, link.Id);

        await unitOfWork.CommitAsync();

        return Result.Ok(link.ToLinkResponse());
    }
}