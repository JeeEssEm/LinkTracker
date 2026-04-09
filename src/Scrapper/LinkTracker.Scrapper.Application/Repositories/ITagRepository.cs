using FluentResults;
using LinkTracker.Scrapper.Domain.Tag.Entities;

namespace LinkTracker.Scrapper.Application.Repositories;

public interface ITagRepository
{
    Task<List<Tag>> CreateTags(IEnumerable<string> tagNames, long chatId);
    Task<List<Tag>> GetTags(long chatId, long linkId);
    Task<List<Tag>> GetTags(long chatId, IEnumerable<string> tagNames);
    Task ConnectTagsToLink(long linkId, IEnumerable<long> tagsIds);
    Task RemoveConnectionsTagsToLink(long chatId, long linkId);
    Task RemoveTags(IEnumerable<long> tagIds);
}
