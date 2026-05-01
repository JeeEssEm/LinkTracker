namespace LinkTracker.Scrapper.Infrastructure.Orm.Models.Sources.Github;


public class GithubRepository
{
    public int Id { get; set; }
    public Guid LinkId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;

    public Link Link { get; set; } = null!;
    public ICollection<GithubIssue> Issues { get; set; } = new List<GithubIssue>();
    public ICollection<GithubPullRequest> PullRequests { get; set; } = new List<GithubPullRequest>();
}
