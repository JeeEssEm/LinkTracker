using Telegram.Bot.Types.Enums;

namespace LinkTracker.Bot.Core.HandlerTypes.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class UpdateHandlerAttribute(UpdateType updateType) : Attribute
{
    public UpdateType UpdateType = updateType;
}