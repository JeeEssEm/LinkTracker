using System.Diagnostics;
using LinkTracker.Scrapper.Domain.Tracking.Abstractions;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;

namespace LinkTracker.Scrapper.Domain.Tracking.SourceHandlers.StackOverflow;

public class StackOverflowSourceHandler(IStackOverflowRepository soRepo) : ILinkSourceHandler
{
    public bool CanHandle(Uri uri)
    {
        if (!uri.Host.Equals("stackoverflow.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return ParseQuestionId(uri).HasValue;
    }

    public LinkSourceType Type => LinkSourceType.StackOverflow;

    public async Task SaveMetadataAsync(LinkId linkId, Uri uri)
    {
        var questionId = ParseQuestionId(uri);
        if (!questionId.HasValue)
        {
            throw new UnreachableException("This method was called without checking with CanHandle!");
        }

        await soRepo.CreateQuestionAsync(linkId, questionId.Value);
    }

    private static long? ParseQuestionId(Uri uri)
    {
        var segments = uri.Segments
            .Select(s => s.TrimEnd('/'))
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        if (segments.Count >= 2 && long.TryParse(segments[1], out var id))
        {
            return id;
        }

        return null;
    }
}
