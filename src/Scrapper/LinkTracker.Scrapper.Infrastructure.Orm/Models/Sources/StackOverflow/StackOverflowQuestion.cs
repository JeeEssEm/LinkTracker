namespace LinkTracker.Scrapper.Infrastructure.Orm.Models.Sources.StackOverflow;

public class StackOverflowQuestion
{
    public long Id { get; set; }
    public Guid LinkId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Link Link { get; set; } = null!;
    public ICollection<StackOverflowAnswer> Answers { get; set; } = new List<StackOverflowAnswer>();
    public ICollection<StackOverflowQuestionComment> Comments { get; set; } = new List<StackOverflowQuestionComment>();
}
