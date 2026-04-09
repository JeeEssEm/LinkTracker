namespace LinkTracker.Bot.Core.StateMachine.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class RequireStateAttribute<TState> : Attribute, IStateAttribute where TState : Enum
{
    public string ExpectedState { get; }

    public RequireStateAttribute(TState state)
    {
        ExpectedState = StateHelpers.GetStateCode(state);
    }
}