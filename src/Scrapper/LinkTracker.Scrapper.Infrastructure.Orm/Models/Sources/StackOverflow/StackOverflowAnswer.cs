namespace LinkTracker.Scrapper.Infrastructure.Orm.Models.Sources.StackOverflow;

public class StackOverflowAnswer
{
    public long Id { get; set; }
    public long QuestionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public StackOverflowQuestion Question { get; set; } = null!;
    public ICollection<StackOverflowComment> Comments { get; set; } = new List<StackOverflowComment>();
}
