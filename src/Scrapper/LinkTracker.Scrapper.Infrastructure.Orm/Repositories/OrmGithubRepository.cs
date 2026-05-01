using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.SourceHandlers.Github;
using LinkTracker.Scrapper.Infrastructure.Orm.Context;
using LinkTracker.Scrapper.Infrastructure.Orm.Models.Sources.Github;
using Microsoft.EntityFrameworkCore;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Repositories;

public class OrmGithubRepository(LinkTrackerContext dbContext) : IGithubRepository
{
    public async Task CreateAsync(LinkId linkId, string name, string ownerName)
    {
        if (await dbContext.GithubRepositories.CountAsync(r => r.LinkId == linkId.Value) == 0)
        {
            var repo = new GithubRepository { LinkId = linkId.Value, Name = name, Owner = ownerName };
            dbContext.GithubRepositories.Add(repo);
            await dbContext.SaveChangesAsync();
        }
    }
}
