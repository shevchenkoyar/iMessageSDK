namespace iMessageSDK.Messages;

/// <summary>
/// The kind of tapback (quick reaction) applied to a message.
/// </summary>
public enum TapbackKind
{
    /// <summary>A heart ("loved") tapback.</summary>
    Loved,

    /// <summary>A thumbs-up ("liked") tapback.</summary>
    Liked,

    /// <summary>A thumbs-down ("disliked") tapback.</summary>
    Disliked,

    /// <summary>A "ha ha" (laughed) tapback.</summary>
    Laughed,

    /// <summary>An exclamation ("emphasized") tapback.</summary>
    Emphasized,

    /// <summary>A question mark ("questioned") tapback.</summary>
    Questioned,

    /// <summary>
    /// A custom emoji tapback (introduced in iOS 18). <see cref="Reaction.EmojiValue"/> carries
    /// the specific emoji.
    /// </summary>
    Emoji,
}
