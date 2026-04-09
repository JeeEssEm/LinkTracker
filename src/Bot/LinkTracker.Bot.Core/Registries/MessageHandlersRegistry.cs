using System.Diagnostics;
using System.Reflection;
using LinkTracker.Bot.Core.HandlerTypes.Attributes;
using LinkTracker.Bot.Core.Registries.Descriptors;
using LinkTracker.Bot.Core.StateMachine;
using Telegram.Bot.Types.Enums;

namespace LinkTracker.Bot.Core.Registries;

public class MessageHandlersRegistry
{
    private readonly Dictionary<string, MessageHandlerDescriptor> _stateBasedHandlers = new();

    public void RegisterHandler<THandler>() => RegisterHandler(typeof(THandler));

    public void RegisterHandler(Type type)
    {
        var state = StateHelpers.GetStateFromAttribute(type);
        if (state == null)
        {
            throw new ArgumentException("Cannot register message handler without state requirement");
        }

        _stateBasedHandlers.Add(state, new MessageHandlerDescriptor(type, ExtractUpdateType(type), state));
    }

    public MessageHandlerDescriptor? SearchHandler(string state)
    {
        return _stateBasedHandlers.GetValueOrDefault(state);
    }

    private UpdateType ExtractUpdateType(Type type)
    {
        var attr = type.GetCustomAttribute(typeof(UpdateHandlerAttribute));
        if (attr == null)
        {
            throw new ArgumentException("Message handler does not contains attribute UpdateHandlerAttribute");
        }

        return ((UpdateHandlerAttribute)attr).UpdateType;
    }
}