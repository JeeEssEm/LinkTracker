using Telegram.Bot.Types;

namespace LinkTracker.Bot.Core.Registries.Descriptors;

public record CommandDescriptor(string Name, string Description, Type CommandType, string? Status = null);