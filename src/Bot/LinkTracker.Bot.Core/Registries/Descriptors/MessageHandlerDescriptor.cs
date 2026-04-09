using Telegram.Bot.Types.Enums;

namespace LinkTracker.Bot.Core.Registries.Descriptors;

public record MessageHandlerDescriptor(Type HandlerType, UpdateType UpdateType, string? State);
