namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.Shared;

public static class TextUtilities
{
    public static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "...";
}
