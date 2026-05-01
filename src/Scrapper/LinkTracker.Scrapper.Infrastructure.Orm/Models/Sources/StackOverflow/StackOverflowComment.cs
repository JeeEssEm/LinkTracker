namespace LinkTracker.Scrapper.Infrastructure.Orm.Models.Sources.StackOverflow;

public class StackOverflowComment
{
    public long Id { get; set; }
    public long AnswerId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public StackOverflowAnswer Answer { get; set; } = null!;
}
