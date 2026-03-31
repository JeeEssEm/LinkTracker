using System.Reflection;
using System.Text;
using LinkTracker.Bot.Core.HandlerTypes;
using LinkTracker.Bot.Core.HandlerTypes.Attributes;
using LinkTracker.Bot.Core.Registries.Descriptors;
using LinkTracker.Bot.Core.StateMachine;
using Telegram.Bot.Types;

namespace LinkTracker.Bot.Core.Registries;

public class CommandRegistry
{
    private readonly Dictionary<string, List<CommandDescriptor>> _commands = new();

    public void RegisterCommand<TCommand>() where TCommand : IMessageHandler
        => RegisterCommand(typeof(TCommand));

    public void RegisterCommand(Type commandType)
    {
        var (name, description) = ExtractCommandMetadata(commandType);

        if (_commands.ContainsKey(name))
        {
            throw new ArgumentException($"Command with name `{name}` already added!");
        }

        var requiredState = StateHelpers.GetStateFromAttribute(commandType);
        var descriptor = new CommandDescriptor(name, description, commandType, requiredState);

        if (_commands.TryGetValue(name, out var descriptors))
        {
            descriptors.Add(descriptor);
        }
        else
        {
            _commands[name] = [descriptor];
        }
    }

    public CommandDescriptor? SearchCommand(string message, string? status = null)
    {
        var command = ExtractCommandName(message);
        if (_commands.TryGetValue(command, out var descriptors))
        {
            var specific = descriptors.FirstOrDefault(x => x.Status == status);
            if (specific != null)
            {
                return specific;
            }

            return descriptors.FirstOrDefault(d => d.Status == null);
        }

        return null;
    }


    public List<string> GetCommandDescriptionsList()
    {
        return GetTypedCommands((name, desc) => $"- /{name} - {desc}");
    }

    public List<BotCommand> GetBotCommands()
    {
        return GetTypedCommands((name, desc) => new BotCommand { Command = name, Description = desc });
    }

    private List<T> GetTypedCommands<T>(Func<string, string, T> predicate)
    {
        return _commands.Values
            .SelectMany(list => list)
            .GroupBy(cmd => cmd.Name.ToLowerInvariant())
            .Select(group =>
            {
                var primary = group.FirstOrDefault(c => c.Status == null) ?? group.First();
                var desc = string.IsNullOrWhiteSpace(primary.Description) ? "Нет описания" : primary.Description;
                return predicate.Invoke(primary.Name, desc);
            })
            .ToList();
    }

    private string ExtractCommandName(string message)
    {
        var spaceIndex = message.IndexOf(' ');
        return spaceIndex == -1 ? message[1..] : message[1..spaceIndex];
    }

    private (string Name, string Description) ExtractCommandMetadata(Type commandType)
    {
        var attr = commandType.GetCustomAttribute(typeof(BotCommandAttribute));
        if (attr == null)
        {
            throw new ArgumentException($"Cannot register {commandType}, it does not has metadata attribute");
        }

        var data = (BotCommandAttribute)attr;
        return (Name: data.Name, Description: data.Description);
    }
}