using System.Reflection;
using LinkTracker.Bot.Core.HandlerTypes;
using LinkTracker.Bot.Core.HandlerTypes.Attributes;
using LinkTracker.Bot.Core.Registries.Descriptors;
using LinkTracker.Bot.Core.StateMachine;
using Telegram.Bot.Types;

namespace LinkTracker.Bot.Core.Registries;

public class CallbackQueryRegistry
{
    private readonly Dictionary<string, CallbackQueryDescriptor> _callbackQueries = new();

    public void RegisterCallbackQuery<TCallbackQuery>() where TCallbackQuery : ICallbackQuery => RegisterCallbackQuery(typeof(TCallbackQuery));

    public void RegisterCallbackQuery(Type callbackQueryType)
    {
        var (pattern, trigger) = ExtractCallbackQueryMetadata(callbackQueryType);

        if (_callbackQueries.ContainsKey(pattern))
        {
            throw new ArgumentException($"CallbackQuery with pattern `{pattern}` already added!");
        }

        var requiredState = StateHelpers.GetStateFromAttribute(callbackQueryType);

        _callbackQueries.Add(
            pattern,

            new CallbackQueryDescriptor(
                pattern,
                trigger,
                callbackQueryType,
                requiredState
        ));
    }

    public CallbackQueryDescriptor? SearchCallbackQuery(CallbackQuery cb, string? state = null)
    {
        return _callbackQueries.Values.FirstOrDefault(descriptor => descriptor.Trigger(cb) && descriptor.State == state);
    }

    private (string pattern, Func<CallbackQuery, bool> trigger) ExtractCallbackQueryMetadata(Type callbackQueryType)
    {
        var attr = callbackQueryType.GetCustomAttribute(typeof(BotCallbackQueryAttribute));
        if (attr == null)
        {
            throw new ArgumentException($"Cannot register {callbackQueryType}, it does not has metadata attribute");
        }

        var data = (BotCallbackQueryAttribute)attr;
        return (pattern: data.Pattern, data.Trigger);
    }
}