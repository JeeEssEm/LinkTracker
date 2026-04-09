using System.Reflection;
using LinkTracker.Bot.Core.StateMachine.Attributes;

namespace LinkTracker.Bot.Core.StateMachine;

public static class StateHelpers
{
    public static string GetStateCode<T>(T state) where T : Enum => $"{typeof(T)}.{state}";

    public static string? GetStateFromAttribute<T>()
        => GetStateFromAttribute(typeof(T));

    public static string? GetStateFromAttribute(Type type)
    {
        var attributes = type.GetCustomAttributes();
        var stateAttribute = attributes.OfType<IStateAttribute>().FirstOrDefault();

        return stateAttribute?.ExpectedState;
    }
}