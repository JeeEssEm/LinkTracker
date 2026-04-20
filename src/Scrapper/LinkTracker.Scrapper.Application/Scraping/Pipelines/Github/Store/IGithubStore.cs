using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Data;
using System.Threading.Tasks;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store.ReadModels;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store;

public interface IGithubStore
{
    Task<IssueData[]> CreateAndReturnIfNotExist(IssueData[] issues);
    Task<PullRequestData[]> CreateAndReturnIfNotExist(PullRequestData[] pullRequests);

    Task<bool> RemoveRepositoryAsync(long id);

    Task<Dictionary<long, RepositoryModel>> GetRepositoriesByLinkIdsAsync(IEnumerable<long> linkIds);
    Task CreateRepository(long linkId, string name, string owner);
}
