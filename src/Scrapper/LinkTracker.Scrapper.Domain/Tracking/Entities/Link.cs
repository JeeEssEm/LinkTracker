using FluentResults;
using LinkTracker.Scrapper.Domain.Tracking.Errors;
using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;

namespace LinkTracker.Scrapper.Domain.Tracking.Entities;

public record struct LinkId(Guid Value)
{
    public LinkId() : this(Guid.NewGuid())
    { }
}

public class Link(LinkId id, Uri uri, LinkSourceType sourceType, DateTimeOffset? lastCheckAt)
{
    public LinkId Id { get; private set; } = id;
    public Uri LinkUri { get; private set; } = uri;
    public LinkSourceType SourceType { get; private set; } = sourceType;
    public DateTimeOffset? LastCheckedAt = lastCheckAt;

    public static Result<Uri> ParseUri(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return Result.Ok(uri);
        }

        return Result.Fail(new ParseUrlError());
    }
}
