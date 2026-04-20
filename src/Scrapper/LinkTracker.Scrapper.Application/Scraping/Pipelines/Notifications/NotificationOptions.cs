namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;

public class NotificationOptions
{
    public const string SectionName = "Notifications";
    public string Transport { get; set; } = "Http";
}