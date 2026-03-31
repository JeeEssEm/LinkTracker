using FluentResults;
using LinkTracker.Scrapper.Domain;
using LinkTracker.Scrapper.Features.Shared.Abstractions;
using LinkTracker.Scrapper.Features.Shared.Dtos;
using LinkTracker.Scrapper.Features.Shared.Errors.Chat;
using LinkTracker.Scrapper.Features.Shared.Errors.Link;

namespace LinkTracker.Scrapper.Features.Links.RemoveLink;

public class RemoveLinkHandler(ILinkStore linkStore, IChatStore chatStore)
{
    public async Task<Result<LinkResponse>> HandleAsync(RemoveLinkRequest request, long chatId)
    {
        var chatRes = await chatStore.GetChatAsync(chatId);
        if (chatRes.IsFailed)
        {
            return Result.Fail(new ChatNotFoundError());
        }

        var linkRes = await linkStore.GetLinkAsync(request.Link, chatId);
        if (linkRes.IsFailed)
        {
            return Result.Fail(new LinkNotFoundError());
        }

        var tempRes = await linkStore.RemoveLink(request.Link, chatId);
        if (tempRes.IsFailed)
        {
            return tempRes.ToResult();
        }
        return LinkResponse.FromDomain(tempRes.Value);
    }
}