using System.Collections.Concurrent;
using LinkTracker.Scrapper.Domain;

namespace LinkTracker.Scrapper.Infrastructure.Persistence;

public class InMemoryDataStorage
{
    public ConcurrentDictionary<long, Chat> Chats { get; } = new();
    public ConcurrentDictionary<(string Address, long OwnerId), Link> Links { get; } = new();
}