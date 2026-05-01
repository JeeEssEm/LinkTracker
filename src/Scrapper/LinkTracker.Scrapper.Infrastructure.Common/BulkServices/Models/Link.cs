namespace LinkTracker.Scrapper.Infrastructure.Pipelines.BulkServices.Models;

public class Link
{
    public Guid Id { get; set; }

    public string Url { get; set; }

    public int SourceType { get; set; }

    public DateTimeOffset? LastCheckAt { get; set; }
}
