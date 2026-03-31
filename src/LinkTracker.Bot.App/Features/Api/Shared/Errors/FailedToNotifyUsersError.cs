using FluentResults;

namespace LinkTracker.Bot.App.Features.Api.Shared.Errors;

public class FailedToNotifyUsersError(List<long> chatIds) : Error()
{
    public List<long> ChatIds => chatIds;
}