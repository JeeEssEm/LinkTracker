namespace LinkTracker.Scrapper.Infrastructure.Orm.Models.Sources.StackOverflow;

public class StackOverflowQuestionComment
{
    public long Id { get; set; }
    public long QuestionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public StackOverflowQuestion Question { get; set; } = null!;
}
