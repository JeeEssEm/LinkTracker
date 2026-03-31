using Telegram.Bot.Types;

namespace LinkTracker.Bot.Core.HandlerTypes.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class BotCallbackQueryAttribute : Attribute
{
    public string Pattern { get; }
    public Func<CallbackQuery, bool> Trigger { get; }

    public BotCallbackQueryAttribute(string pattern)
    {
        Pattern = pattern;
        Trigger = cb => cb.Data == pattern;
    }

    public BotCallbackQueryAttribute(string pattern, Func<CallbackQuery, bool> shouldTrigger)
    {
        Pattern = pattern;
        Trigger = shouldTrigger;
    }
}
