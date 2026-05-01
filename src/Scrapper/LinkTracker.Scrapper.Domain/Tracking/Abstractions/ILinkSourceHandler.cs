using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;

namespace LinkTracker.Scrapper.Domain.Tracking.Abstractions;

public interface ILinkSourceHandler
{
    bool CanHandle(Uri uri);
    LinkSourceType Type { get; }
    Task SaveMetadataAsync(LinkId linkId, Uri uri);
}
