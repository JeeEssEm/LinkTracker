using System.Collections.Concurrent;
using Confluent.Kafka;
using LinkTracker.Scrapper.Contracts.Dtos.Notifications;

namespace AiAgentService.Web.Infrastructure.MessageAcknowledgment;


public class MessageAcknowledgementManager(ILogger<MessageAcknowledgementManager> logger)
{
    private readonly ConcurrentDictionary<TopicPartitionOffset, MessageTracker> _messagesChatsMap = new();

    public void Update(UpdateMessage message, TopicPartitionOffset key)
    {
        _messagesChatsMap[key] = new MessageTracker(message.TgChatIds);
    }

    public List<TopicPartitionOffset> Commit(IEnumerable<TopicPartitionOffset> messageIds, IEnumerable<long> chatIds)
    {
        var chats = chatIds.ToHashSet();
        var doneMessages = new List<TopicPartitionOffset>();

        foreach (var messageId in messageIds)
        {
            if (!_messagesChatsMap.TryGetValue(messageId, out var trackedChats))
            {
                logger.LogWarning("Cannot find message in map! Id={id}, Items={items}", messageId, _messagesChatsMap);
                continue;
            }

            if (trackedChats.RemoveChatsAndCheckIfEmpty(chats))
            {
                doneMessages.Add(messageId);
                _messagesChatsMap.Remove(messageId, out _);
            }
        }

        return doneMessages;
    }
}
