namespace LinkTracker.Scrapper.Infrastructure.Orm.Models.Sources.Github;

public class GithubPullRequest
{
    public int GithubRepoId { get; set; }
    public int Number { get; set; }
    public DateTime CreatedAt { get; set; }
    public string PreviousState { get; set; } = string.Empty;

    public GithubRepository Repository { get; set; } = null!;
}
