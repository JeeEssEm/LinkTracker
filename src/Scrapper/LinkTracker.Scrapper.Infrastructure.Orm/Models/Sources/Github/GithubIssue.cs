namespace LinkTracker.Scrapper.Infrastructure.Orm.Models.Sources.Github;

public class GithubIssue
{
    public int GitHubRepoId { get; set; }
    public int Number { get; set; }
    public DateTime CreatedAt { get; set; }
    public string PreviousStatus { get; set; } = string.Empty;

    public GithubRepository Repository { get; set; } = null!;
}
