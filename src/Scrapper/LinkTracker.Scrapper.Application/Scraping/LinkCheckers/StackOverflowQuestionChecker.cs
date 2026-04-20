namespace LinkTracker.Scrapper.Application.Scraping.LinkCheckers;

public class StackOverflowQuestionChecker : ILinkChecker
{
    public const string ConstKey = "stackoverflow-question";

    public string Key => ConstKey;

    public string SupportedUrlType => "https://stackoverflow.com/questions/{question_id}";

    public bool CanHandle(Uri url)
    {
        if (!url.Host.Equals("stackoverflow.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return TryParseQuestionId(url, out _);
    }

    private static bool TryParseQuestionId(Uri url, out long questionId)
    {
        questionId = 0;

        // stackoverflow.com/questions/{id}/...
        var segments = url.Segments
            .Select(s => s.TrimEnd('/'))
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        if (segments.Count < 2 ||
            !segments[0].Equals("questions", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return long.TryParse(segments[1], out questionId);
    }
}