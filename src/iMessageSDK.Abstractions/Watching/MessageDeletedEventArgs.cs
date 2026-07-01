using iMessageSDK.Chats;
using iMessageSDK.Messages;

namespace iMessageSDK.Watching;

/// <summary>Event data for <see cref="IMessageWatcher.MessageDeleted"/>.</summary>
public sealed class MessageDeletedEventArgs : EventArgs
{
    /// <summary>The identifier of the message that was unsent (deleted).</summary>
    public required MessageId MessageId { get; init; }

    /// <summary>The identifier of the chat the message belonged to.</summary>
    public required ChatId ChatId { get; init; }

    /// <summary>The moment the message was unsent.</summary>
    public required DateTimeOffset DeletedAt { get; init; }
}
