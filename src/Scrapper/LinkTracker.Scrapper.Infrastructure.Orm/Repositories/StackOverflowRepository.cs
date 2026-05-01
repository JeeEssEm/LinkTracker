using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.SourceHandlers.StackOverflow;
using LinkTracker.Scrapper.Infrastructure.Orm.Context;
using LinkTracker.Scrapper.Infrastructure.Orm.Models.Sources.StackOverflow;
using Microsoft.EntityFrameworkCore;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Repositories;

public class StackOverflowRepository(LinkTrackerContext dbContext) : IStackOverflowRepository
{
    public async Task CreateQuestionAsync(LinkId linkId, long questionId)
    {
        if (await dbContext.StackOverflowQuestions.CountAsync(q => q.LinkId == linkId.Value) == 0)
        {
            var question = new StackOverflowQuestion() { Id = questionId, LinkId = linkId.Value, };
            dbContext.Add(question);
            await dbContext.SaveChangesAsync();
        }
    }
}