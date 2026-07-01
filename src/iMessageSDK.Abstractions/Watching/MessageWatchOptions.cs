using iMessageSDK.Chats;

namespace iMessageSDK.Watching;

/// <summary>
/// Options controlling which events an <see cref="IMessageWatcher"/> observes.
/// </summary>
public sealed record MessageWatchOptions
{
    /// <summary>
    /// Restricts observation to a single chat. <see langword="null"/> (the default) observes
    /// every chat.
    /// </summary>
    public ChatId? ChatId { get; init; }
}
