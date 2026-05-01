using FluentResults;
using LinkTracker.Scrapper.Application.Abstractions.UnitOfWork;
using LinkTracker.Scrapper.Contracts.Dtos.Links;
using LinkTracker.Scrapper.Contracts.Errors;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
using LinkTracker.Scrapper.Domain.Tracking.Services;
using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;
using LinkTracker.Scrapper.Errors.Chats;
using LinkTracker.Scrapper.Errors.Links;
using Microsoft.Extensions.Logging;

namespace LinkTracker.Scrapper.Application.Handlers.Links;

public class AddLinkHandler(LinkManager linkManager, IChatRepository chatRepo, ISubscriptionRepository subRepo, IUnitOfWork uow, ILogger<AddLinkHandler> logger)
{
    public async Task<Result<LinkResponse>> HandleAsync(AddLinkRequest request, long chatId)
    {
        var parsedUrlResult = Link.ParseUri(request.Link);
        if (parsedUrlResult.IsFailed)
        {
            return Result.Fail(new IncorrectRequestParameters($"Cannot parse url = {request.Link}"));
        }

        if (!await chatRepo.ExistsAsync(new ChatId(chatId)))
        {
            return Result.Fail(new ChatNotFound());
        }

        try
        {
            await uow.BeginAsync();
            var linkResult = await linkManager.GetOrCreateAsync(parsedUrlResult.Value);
            if (linkResult.IsFailed)
            {
                await uow.RollbackAsync();
                return linkResult.ToResult();
            }

            var link = linkResult.Value;
            var sub = new Subscription(new SubscriptionId(link.Id, new ChatId(chatId)), request.Tags.Select(t => new Tag(t)));

            var subscribed = await subRepo.CreateAsync(sub);
            if (!subscribed)
            {
                await uow.RollbackAsync();
                return Result.Fail(new LinkAlreadyExists());
            }

            await uow.CommitAsync();

            return Result.Ok(new LinkResponse(link.Id.Value, request.Link, request.Tags));
        }
        catch (Exception e)
        {
            await uow.RollbackAsync();
            logger.LogError(e, "Something went wrong while adding new link");
            throw;
        }
    }
}