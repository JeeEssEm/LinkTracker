using LinkTracker.Scrapper.Domain.Tracking.Entities;

namespace LinkTracker.Scrapper.Domain.Tracking.SourceHandlers.StackOverflow;

public interface IStackOverflowRepository
{
    Task CreateQuestionAsync(LinkId linkId, long questionId);
}
