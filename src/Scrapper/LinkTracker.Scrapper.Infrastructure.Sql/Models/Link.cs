namespace LinkTracker.Scrapper.Infrastructure.Sql.Models;

public class Link
{
    public long Id { get; set; }

    public string Url { get; set; }

    public string CheckerKey { get; set; }

    public DateTime? LastCheckAt { get; set; }
    public DateTime? LastChangeAt { get; set; }
}
