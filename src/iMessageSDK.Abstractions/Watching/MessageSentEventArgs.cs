using iMessageSDK.Messages;

namespace iMessageSDK.Watching;

/// <summary>Event data for <see cref="IMessageWatcher.MessageSent"/>.</summary>
public sealed class MessageSentEventArgs : EventArgs
{
    /// <summary>The message that was sent.</summary>
    public required Message Message { get; init; }
}
