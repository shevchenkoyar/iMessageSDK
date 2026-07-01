using iMessageSDK.Messages;

namespace iMessageSDK.Watching;

/// <summary>Event data for <see cref="IMessageWatcher.MessageEdited"/>.</summary>
public sealed class MessageEditedEventArgs : EventArgs
{
    /// <summary>The message in its current (edited) state.</summary>
    public required Message Message { get; init; }

    /// <summary>The message's text before this edit, if known.</summary>
    public string? PreviousText { get; init; }
}
