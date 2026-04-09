using LinkTracker.Scrapper.Domain.Tag.Entities;

namespace LinkTracker.Scrapper.Infrastructure.Orm.ModelExtensions;

public static class TagsExtensions
{
    public static List<Tag> ToDomain(this IEnumerable<Models.Tag> tags)
        => tags.Select(t => new Tag(t.Id, t.Name)).ToList();
}