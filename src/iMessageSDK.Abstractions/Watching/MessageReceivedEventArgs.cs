using iMessageSDK.Messages;

namespace iMessageSDK.Watching;

/// <summary>Event data for <see cref="IMessageWatcher.MessageReceived"/>.</summary>
public sealed class MessageReceivedEventArgs : EventArgs
{
    /// <summary>The message that was received.</summary>
    public required Message Message { get; init; }
}
