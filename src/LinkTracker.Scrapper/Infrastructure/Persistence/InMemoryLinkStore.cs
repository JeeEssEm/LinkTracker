using FluentResults;
using LinkTracker.Scrapper.Domain;
using LinkTracker.Scrapper.Features.Shared.Abstractions;

namespace LinkTracker.Scrapper.Infrastructure.Persistence;

public class InMemoryLinkStore(InMemoryDataStorage storage) : ILinkStore
{
    private long _nextId;

    public Task<Result<Link>> GetLinkAsync(string address, long ownerId)
    {
        if (storage.Links.TryGetValue((address, ownerId), out var link))
        {
            return Task.FromResult(Result.Ok(link));
        }

        return Task.FromResult(Result.Fail<Link>("Link not found"));
    }

    public Task<List<Link>> GetLinksByOwnerIdAsync(long ownerId)
    {
        var links = storage.Links.Values.Where(l => l.Owner.Id == ownerId).ToList();
        return Task.FromResult(links);
    }

    public Task<Result<Link>> AddLink(Link link)
    {
        var id = Interlocked.Increment(ref _nextId);
        var newLink = link with { Id = id };

        if (!storage.Links.TryAdd((link.Url, link.Owner.Id), newLink))
        {
            return Task.FromResult(Result.Fail<Link>("Link already exists"));
        }

        return Task.FromResult(Result.Ok(newLink));
    }

    public Task<List<Link>> GetAllLinksAsync()
    {
        return Task.FromResult(storage.Links.Values.ToList());
    }

    public Task<Result<Link>> RemoveLink(string address, long ownerId)
    {
        if (storage.Links.TryRemove((address, ownerId), out var link))
        {
            return Task.FromResult(Result.Ok(link));
        }

        return Task.FromResult(Result.Fail<Link>("Link not found"));
    }

    public Task<Result<Link>> UpdateLastCheckedAt(string address, long ownerId, DateTimeOffset checkedAt)
    {
        if (!storage.Links.TryGetValue((address, ownerId), out var link))
        {
            return Task.FromResult(Result.Fail<Link>("Link not found"));
        }

        var updated = link with { LastCheckedAt = checkedAt };
        storage.Links[(address, ownerId)] = updated;
        return Task.FromResult(Result.Ok(updated));
    }
}
