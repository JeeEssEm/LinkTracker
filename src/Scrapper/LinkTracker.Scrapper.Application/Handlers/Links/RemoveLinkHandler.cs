using FluentResults;
using LinkTracker.Scrapper.Contracts.Dtos.Links;
using LinkTracker.Scrapper.Contracts.GrpcErrorProfiles;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
using LinkTracker.Scrapper.Errors.Chats;
using LinkTracker.Scrapper.Errors.Links;

namespace LinkTracker.Scrapper.Application.Handlers.Links;

public class RemoveLinkHandler(ISubscriptionRepository subRepo, IChatRepository chatRepo)
{
    public async Task<Result<LinkResponse>> HandleAsync(RemoveLinkRequest request, long chatId)
    {
        if (!await chatRepo.ExistsAsync(new ChatId(chatId)))
        {
            return Result.Fail(new ChatNotFound());
        }

        var sub = await subRepo.GetAsync(new ChatId(chatId), request.Link);
        if (sub == null)
        {
            return Result.Fail(new LinkNotFound());
        }

        await subRepo.RemoveAsync(sub);

        return Result.Ok(new LinkResponse(sub.Id.LinkId.Value, request.Link, sub.Tags.Select(t => t.Name).ToList().AsReadOnly()));
    }
}