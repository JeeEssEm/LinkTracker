using Telegram.Bot.Types;

namespace LinkTracker.Bot.Core.Registries.Descriptors;

public record CallbackQueryDescriptor(string Pattern, Func<CallbackQuery, bool> Trigger, Type CallbackQueryType, string? State = null);