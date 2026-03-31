using FluentResults;
using LinkTracker.Scrapper.Domain;

namespace LinkTracker.Scrapper.Features.Shared.Abstractions;

public interface ILinkStore
{
    Task<Result<Link>> GetLinkAsync(string address, long ownerId);
    Task<List<Link>> GetLinksByOwnerIdAsync(long ownerId);
    Task<List<Link>> GetAllLinksAsync();
    Task<Result<Link>> AddLink(Link link);
    Task<Result<Link>> RemoveLink(string address, long ownerId);
    Task<Result<Link>> UpdateLastCheckedAt(string address, long ownerId, DateTimeOffset checkedAt);
}
