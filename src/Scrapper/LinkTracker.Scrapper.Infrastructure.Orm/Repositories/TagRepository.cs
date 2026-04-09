using FluentResults;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Infrastructure.Orm.Context;
using LinkTracker.Scrapper.Infrastructure.Orm.ModelExtensions;
using LinkTracker.Scrapper.Infrastructure.Orm.Models;
using Microsoft.EntityFrameworkCore;
using Tag = LinkTracker.Scrapper.Domain.Tag.Entities.Tag;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Repositories;

public class TagRepository(LinkTrackerContext dbContext) : ITagRepository
{
    public async Task<List<Tag>> CreateTags(IEnumerable<string> tagNames, long chatId)
    {
        var tagModels = tagNames.Select(t => new Models.Tag() { Name = t, ChatId = chatId }).ToList();
        await dbContext.AddRangeAsync(tagModels);
        await dbContext.SaveChangesAsync();

        return tagModels.Select(t => new Tag(t.Id, t.Name)).ToList();
    }

    public async Task<List<Tag>> GetTags(long chatId, long linkId)
    {
        var tags = await dbContext.Tags.Where(t => t.ChatId == chatId && t.LinkTags.Any(lt => lt.LinkId == linkId)).ToListAsync();

        return tags.ToDomain();
    }

    public async Task<List<Tag>> GetTags(long chatId, IEnumerable<string> tagNames)
    {
        var tags = await dbContext.Tags.Where(
            t => t.ChatId == chatId && tagNames.Contains(t.Name))
            .ToListAsync();
        return tags.ToDomain();
    }

    public async Task ConnectTagsToLink(long linkId, IEnumerable<long> tagsIds)
    {
        var connections = tagsIds.Select(id => new LinkTag { TagId = id, LinkId = linkId });
        await dbContext.AddRangeAsync(connections);
        await dbContext.SaveChangesAsync();
    }

    public async Task RemoveConnectionsTagsToLink(long chatId, long linkId)
    {
        var connections = await dbContext.LinkTags.Where(lt => lt.LinkId == linkId && lt.Tag.ChatId == chatId).ToListAsync();
        dbContext.LinkTags.RemoveRange(connections);
        await dbContext.SaveChangesAsync();
    }

    public async Task RemoveTags(IEnumerable<long> tagIds)
    {
        var tags = await dbContext.Tags.Where(t => tagIds.Contains(t.Id)).ToListAsync();
        dbContext.Tags.RemoveRange(tags);
        await dbContext.SaveChangesAsync();
    }
}