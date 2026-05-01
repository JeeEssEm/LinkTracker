using System.Diagnostics;
using LinkTracker.Scrapper.Domain.Tracking.Abstractions;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;

namespace LinkTracker.Scrapper.Domain.Tracking.SourceHandlers.Github;

public class GithubSourceHandler(IGithubRepository githubRepo) : ILinkSourceHandler
{
    public bool CanHandle(Uri uri)
    {
        if (!uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return ParseLink(uri) != null;
    }

    public LinkSourceType Type => LinkSourceType.Github;

    public async Task SaveMetadataAsync(LinkId linkId, Uri uri)
    {
        var parsedLink = ParseLink(uri);
        if (parsedLink == null)
        {
            throw new UnreachableException("This method was called without checking with CanHandle!");
        }

        await githubRepo.CreateAsync(linkId, parsedLink.Value.Name, parsedLink.Value.Owner);
    }

    private static (string Owner, string Name)? ParseLink(Uri uri)
    {
        var segments = uri.Segments
            .Select(s => s.TrimEnd('/'))
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
        if (segments.Count != 2)
        {
            return null;
        }

        return ValueTuple.Create(segments[0], segments[1]);
    }
}