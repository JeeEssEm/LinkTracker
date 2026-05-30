namespace AiAgentService.Web.Infrastructure.MessageAcknowledgment;

public class MessageTracker(IEnumerable<long> chatIds)
{
    private readonly HashSet<long> _remainingChats = chatIds.ToHashSet();
    private readonly Lock _lock = new();

    public bool RemoveChatsAndCheckIfEmpty(HashSet<long> chatsToRemove)
    {
        lock (_lock)
        {
            _remainingChats.RemoveWhere(chatsToRemove.Contains);
            return _remainingChats.Count == 0;
        }
    }
}