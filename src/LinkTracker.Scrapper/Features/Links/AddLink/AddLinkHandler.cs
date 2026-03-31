using FluentResults;
using LinkTracker.Scrapper.ApiClients;
using LinkTracker.Scrapper.ApiClients.Shared.Errors;
using LinkTracker.Scrapper.Domain;
using LinkTracker.Scrapper.Features.Shared.Abstractions;
using LinkTracker.Scrapper.Features.Shared.Dtos;
using LinkTracker.Scrapper.Features.Shared.Errors.Chat;
using LinkTracker.Scrapper.Features.Shared.Errors.Link;

namespace LinkTracker.Scrapper.Features.Links.AddLink;

public class AddLinkHandler(ILinkStore linkStore, IChatStore chatStore, LinkCheckerResolver checkerResolver)
{
    public async Task<Result<LinkResponse>> HandleAsync(AddLinkRequest request, long chatId)
    {
        var checkerResult = checkerResolver.Resolve(request.Link);
        if (checkerResult.IsFailed)
        {
            return Result.Fail(checkerResult.Errors);
        }

        var chatResult = await chatStore.GetChatAsync(chatId);
        if (chatResult.IsFailed)
        {
            return Result.Fail(new ChatNotFoundError());
        }

        var existingLink = await linkStore.GetLinkAsync(request.Link, chatId);
        if (existingLink.IsSuccess)
        {
            return Result.Fail(new LinkAlreadyExistsError());
        }

        var tempResult = await linkStore.AddLink(new Link(0, request.Link, checkerResult.Value.Key, request.Tags, request.Filters, chatResult.Value));
        if (tempResult.IsFailed)
        {
            return tempResult.ToResult();
        }

        return Result.Ok(new LinkResponse(tempResult.Value.Id, tempResult.Value.Url, tempResult.Value.Tags, tempResult.Value.Filters));
    }
}