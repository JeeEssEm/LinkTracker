using LinkTracker.Scrapper.Domain.Tag.Entities;

namespace LinkTracker.Scrapper.Infrastructure.Sql.ModelExtensions;

public static class TagsExtensions
{
    public static IEnumerable<Tag> ToDomain(this IEnumerable<Models.Tag> tags) => tags.Select(t => new Tag(t.Id, t.Name));
}