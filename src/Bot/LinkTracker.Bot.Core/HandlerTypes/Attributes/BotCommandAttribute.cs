namespace LinkTracker.Bot.Core.HandlerTypes.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class BotCommandAttribute(string name, string description) : Attribute
{
    public string Name { get; private set; } = name;
    public string Description { get; private set; } = description;
}
