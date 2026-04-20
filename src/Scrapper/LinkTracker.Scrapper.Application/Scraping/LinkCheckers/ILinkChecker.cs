namespace LinkTracker.Scrapper.Application.Scraping.LinkCheckers;

public interface ILinkChecker
{
    string Key { get; }
    string SupportedUrlType { get; }
    bool CanHandle(Uri url);
}
