using iMessageSDK.Chats;

namespace iMessageSDK.Messages;

/// <summary>A tapback (reaction) applied to a message.</summary>
public sealed record Reaction
{
    /// <summary>The kind of tapback.</summary>
    public required TapbackKind Kind { get; init; }

    /// <summary>
    /// The specific emoji used, when <see cref="Kind"/> is <see cref="TapbackKind.Emoji"/>;
    /// otherwise <see langword="null"/>.
    /// </summary>
    public string? EmojiValue { get; init; }

    /// <summary>The participant who applied the reaction.</summary>
    public required Participant Sender { get; init; }

    /// <summary>The moment the reaction was applied.</summary>
    public required DateTimeOffset ReactedAt { get; init; }

    /// <summary>
    /// <see langword="true"/> if the sender subsequently removed this reaction. Removed
    /// reactions are retained rather than omitted so consumers can reconcile state across
    /// <see cref="Watching.ReactionAddedEventArgs"/> notifications.
    /// </summary>
    public bool IsRemoved { get; init; }
}
