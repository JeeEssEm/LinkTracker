using FluentResults;

namespace LinkTracker.Bot.Contracts.Errors;

public class FailedToNotifyUsers(List<long> chatIds) : Error()
{
    public List<long> ChatIds => chatIds;
}