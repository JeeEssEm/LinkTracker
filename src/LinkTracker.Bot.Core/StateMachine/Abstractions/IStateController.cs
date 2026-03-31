namespace LinkTracker.Bot.Core.StateMachine.Abstractions;

public interface IStateController
{
    Task SetStateAsync(long chatId, string? state);
    Task<string?> GetStateAsync(long chatId);
}
