using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Data;
using System.Threading.Tasks;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store.ReadModels;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store;

public interface IGithubStore
{
    Task<IssueData[]> CreateAndReturnIfNotExist(IssueData[] issues);
    Task<PullRequestData[]> CreateAndReturnIfNotExist(PullRequestData[] pullRequests);
    Task<Dictionary<Guid, RepositoryModel>> GetRepositoriesByLinkIdsAsync(IEnumerable<Guid> linkIds);
}
