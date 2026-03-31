namespace LinkTracker.Bot.Core.StateMachine.Attributes;

public interface IStateAttribute
{
    public string ExpectedState { get; }
}