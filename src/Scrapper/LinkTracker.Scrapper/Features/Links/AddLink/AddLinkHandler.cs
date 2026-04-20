using System.Diagnostics;
using FluentResults;
using LinkTracker.Scrapper.ApiClients;
using LinkTracker.Scrapper.Application.Abstractions;
using LinkTracker.Scrapper.Application.ReadModels;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.LinkCheckers;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using LinkTracker.Scrapper.Contracts.Dtos.Links;
using LinkTracker.Scrapper.Domain.Links.Entities;
using LinkTracker.Scrapper.Errors.Chats;
using LinkTracker.Scrapper.Errors.Links;
using LinkTracker.Scrapper.Features.Shared;

namespace LinkTracker.Scrapper.Features.Links.AddLink;

public class AddLinkHandler(
    ILinkRepository linkRepository,
    IChatRepository chatRepository,
    ISubscriptionRepository subscriptionRepository,
    ITagRepository tagRepository,
    IUnitOfWork unitOfWork,
    IGithubStore githubStore,
    IStackOverflowStore soStore,
    LinkCheckerResolver checkerResolver,
    ILogger<AddLinkHandler> logger)
{
    public async Task<Result<LinkResponse>> HandleAsync(AddLinkRequest request, long chatId)
    {
        var checkerResult = checkerResolver.Resolve(request.Link); // TODO: нужны чекеры
        if (checkerResult.IsFailed)
        {
            return Result.Fail(checkerResult.Errors);
        }

        var chat = await chatRepository.GetChatAsync(chatId);
        if (chat == null)
        {
            return Result.Fail(new ChatNotFound());
        }

        var result = await HandleCreationAsync(chatId, request.Tags, checkerResult.Value.Key, request.Link);
        if (result.IsFailed)
        {
            return result.ToResult();
        }

        return Result.Ok(result.Value.ToLinkResponse());
    }

    private async Task<Result<SubscribedLink>> HandleCreationAsync(long chatId, List<string> tags, string checkerKey, string url)
    {
        await unitOfWork.BeginAsync();
        try
        {
            var linkResult = await GetOrCreateLink(url, chatId, checkerKey);
            var link = linkResult.Value;
            if (!linkResult.HasSuccess<Created>())
            {
                if (await subscriptionRepository.SubscriptionExistsAsync(chatId, link.Id))
                {
                    return Result.Fail(new LinkAlreadyExists());
                }
            }

            await AddNecessaryTags(tags, chatId, link.Id);
            await subscriptionRepository.CreateSubscriptionAsync(chatId, link.Id);

            await unitOfWork.CommitAsync();

            return Result.Ok(new SubscribedLink(link.Id, link.Url, tags));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to create link");
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    private async Task AddNecessaryTags(List<string> tags, long chatId, long linkId)
    {
        var existingTags = await tagRepository.GetTags(chatId, tags);
        var existingTagsNames = existingTags.Select(t => t.Name).ToHashSet();

        var newTags = await tagRepository.CreateTags(
            tags.Where(t => !existingTagsNames.Contains(t)),
            chatId
        );

        await tagRepository.ConnectTagsToLink(linkId, newTags.Concat(existingTags).Select(t => t.Id));
    }

    private async Task<Result<Link>> GetOrCreateLink(string url, long chatId, string checkerKey)
    {
        var existingLink = await linkRepository.GetLinkAsync(url);
        if (existingLink != null)
        {
            return Result.Ok(existingLink);
        }

        var newLink = await linkRepository.CreateLink(url, checkerKey);
        await CreateCoupledSourceTables(newLink.Id, url, checkerKey);
        return Result.Ok(newLink).WithSuccess<Created>();
    }

    private async Task CreateCoupledSourceTables(long linkId, string url, string checkerKey)
    {
        // TODO: унести в домен и сделать нормальную стратегию
        string[] segments;
        switch (checkerKey)
        {
            case StackOverflowQuestionChecker.ConstKey:
                segments = ParseSegments(url);
                await soStore.CreateQuestion(linkId, long.Parse(segments[1]));
                break;
            case GithubRepositoryChecker.ConstKey:
                segments = ParseSegments(url);
                await githubStore.CreateRepository(linkId, segments[1], segments[0]);
                break;
        }
    }

    private string[] ParseSegments(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.Segments
             .Select(s => s.TrimEnd('/'))
             .Where(s => !string.IsNullOrEmpty(s))
             .ToArray();
        }

        throw new UnreachableException($"How did we get here? It's impossible: {url}");
    }

    private class Created : Success;
}