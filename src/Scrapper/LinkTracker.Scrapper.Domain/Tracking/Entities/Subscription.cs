using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;

namespace LinkTracker.Scrapper.Domain.Tracking.Entities;

public record struct SubscriptionId(LinkId LinkId, ChatId ChatId);


public class Subscription(SubscriptionId subscriptionId, IEnumerable<Tag> tags)
{
    public SubscriptionId Id { get; private set; } = subscriptionId;

    private readonly List<Tag> _tags = tags.ToList();
    public IReadOnlyList<Tag> Tags => _tags;

    public void AddTags(IEnumerable<Tag> tags)
    {
        foreach (var tag in tags)
        {
            if (!_tags.Contains(tag))
            {
                _tags.Add(tag);
            }
        }
    }

    public void RemoveTags(IEnumerable<Tag> tags)
    {
        foreach (var tag in tags)
        {
            _tags.Remove(tag);
        }
    }
}
