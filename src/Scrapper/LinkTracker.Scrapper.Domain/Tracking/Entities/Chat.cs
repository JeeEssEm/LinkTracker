using System.Runtime.Serialization;
using Microsoft.VisualBasic;

namespace LinkTracker.Scrapper.Domain.Tracking.Entities;

public record struct ChatId(long Value);

public class Chat(ChatId id)
{
    public ChatId Id { get; private set; } = id;
}
