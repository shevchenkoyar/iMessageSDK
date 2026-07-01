using iMessageSDK.Messages;

namespace iMessageSDK.Watching;

/// <summary>Event data for <see cref="IMessageWatcher.ReactionAdded"/>.</summary>
public sealed class ReactionAddedEventArgs : EventArgs
{
    /// <summary>The reaction that was applied.</summary>
    public required Reaction Reaction { get; init; }

    /// <summary>The identifier of the message the reaction was applied to.</summary>
    public required MessageId TargetMessageId { get; init; }
}
