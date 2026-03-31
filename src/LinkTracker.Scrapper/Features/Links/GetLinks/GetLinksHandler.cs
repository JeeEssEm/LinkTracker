using FluentResults;
using LinkTracker.Scrapper.Features.Shared.Abstractions;
using LinkTracker.Scrapper.Features.Shared.Dtos;
using LinkTracker.Scrapper.Features.Shared.Errors.Chat;

namespace LinkTracker.Scrapper.Features.Links.GetLinks;

public class GetLinksHandler(ILinkStore linkStore, IChatStore chatStore)
{
    public async Task<Result<ListLinksResponse>> HandleAsync(long chatId)
    {
        var chatRes = await chatStore.GetChatAsync(chatId);
        if (chatRes.IsFailed)
        {
            return Result.Fail(new ChatNotFoundError());
        }

        var linkRes = await linkStore.GetLinksByOwnerIdAsync(chatId);
        var links = linkRes.Select(LinkResponse.FromDomain).ToList();

        return Result.Ok(new ListLinksResponse(links, links.Count));
    }
}