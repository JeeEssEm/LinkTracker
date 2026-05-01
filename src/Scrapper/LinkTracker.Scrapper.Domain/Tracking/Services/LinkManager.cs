using FluentResults;
using LinkTracker.Scrapper.Domain.Tracking.Abstractions;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.Errors;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;

namespace LinkTracker.Scrapper.Domain.Tracking.Services;

public class LinkManager(ILinkRepository linkRepo, IEnumerable<ILinkSourceHandler> sourceHandlers)
{
    public async Task<Result<Link>> GetOrCreateAsync(Uri uri)
    {
        var linkTypeResult = DefineSourceHandler(uri);
        if (linkTypeResult.IsFailed)
        {
            return linkTypeResult.ToResult();
        }

        var handler = linkTypeResult.Value;
        var link = await linkRepo.GetOrCreateAsync(uri, handler.Type);
        await handler.SaveMetadataAsync(link.Id, uri);

        return Result.Ok(link);
    }

    private Result<ILinkSourceHandler> DefineSourceHandler(Uri uri)
    {
        foreach (var linkSourceHandler in sourceHandlers)
        {
            if (linkSourceHandler.CanHandle(uri))
            {
                return Result.Ok(linkSourceHandler);
            }
        }

        return Result.Fail(new UnknownLinkSourceType());
    }
}