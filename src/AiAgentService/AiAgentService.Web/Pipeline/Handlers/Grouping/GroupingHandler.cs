using System.Text;
using AiAgentService.Web.Pipeline.Data;
using Confluent.Kafka;
using LinkTracker.Bot.Contracts.Rest;
using Microsoft.OpenApi.Extensions;

namespace AiAgentService.Web.Pipeline.Handlers.Grouping;

public class GroupingHandler
{
    public static List<GroupedMessages> Group(MessageData[] messages)
    {
        var messagesByChat = new Dictionary<long, List<MessageData>>();

        foreach (var wrap in messages)
        {
            foreach (var chatId in wrap.Message.TgChatIds)
            {
                if (!messagesByChat.TryGetValue(chatId, out var list))
                {
                    list = [];
                    messagesByChat[chatId] = list;
                }
                list.Add(wrap);
            }
        }

        var setComparer = HashSet<TopicPartitionOffset>.CreateSetComparer();
        var groupedBySets = new Dictionary<HashSet<TopicPartitionOffset>, GroupedMessages>(setComparer);

        foreach (var (chatId, chatMessages) in messagesByChat)
        {
            var messageIds = chatMessages.Select(m => m.Offset).ToHashSet();

            if (!groupedBySets.TryGetValue(messageIds, out var payload))
            {
                var (description, priority) = BuildPayload(chatMessages);

                groupedBySets[messageIds] = new GroupedMessages(
                    ChatIds: [chatId],
                    Description: description,
                    Priority: priority,
                    MessageIds: messageIds
                );
            }
            else
            {
                var chatIds = payload.ChatIds.ToList();
                chatIds.Add(chatId);
                groupedBySets[messageIds] = payload with { ChatIds = chatIds };
            }
        }

        return groupedBySets.Values.ToList();
    }

    private static (string Description, string Priority) BuildPayload(List<MessageData> messages)
    {
        if (messages.Count == 1)
        {
            var singleMsg = messages[0];
            return (singleMsg.Message.Description, singleMsg.Priority.ToString());
        }

        var sorted = messages
            .OrderBy(m => m.Priority)
            .ThenBy(m => m.Message.Description)
            .ToList();

        var sb = new StringBuilder();
        for (int i = 0; i < sorted.Count; i++)
        {
            sb.AppendLine($"{i + 1}. {sorted[i].Message.Description}");
        }

        return (sb.ToString().TrimEnd(), nameof(MessagePriority.High));
    }
}