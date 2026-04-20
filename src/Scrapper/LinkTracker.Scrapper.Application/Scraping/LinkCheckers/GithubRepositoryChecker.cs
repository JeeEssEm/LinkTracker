namespace LinkTracker.Scrapper.Application.Scraping.LinkCheckers;

public class GithubRepositoryChecker : ILinkChecker
{
    public const string ConstKey = "github-repository";
    public string Key => ConstKey;

    public string SupportedUrlType => "https://github.com/{user-name}/{repository-name}";

    public bool CanHandle(Uri url)
    {
        if (!url.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var segments = url.Segments
            .Select(s => s.TrimEnd('/'))
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        return segments.Count >= 2;
    }
}