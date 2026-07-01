using iMessageSDK.Chats;

namespace iMessageSDK.Messages;

/// <summary>
/// Describes formatting, a link, or a mention applied to a sub-range of an
/// <see cref="AttributedText.PlainText"/> value.
/// </summary>
public sealed record TextRun
{
    /// <summary>The character range within <see cref="AttributedText.PlainText"/> this run applies to.</summary>
    public required Range Range { get; init; }

    /// <summary>The text styling applied to <see cref="Range"/>, if any.</summary>
    public TextRunStyle Style { get; init; } = TextRunStyle.None;

    /// <summary>The hyperlink applied to <see cref="Range"/>, if this run represents a link.</summary>
    public Uri? Link { get; init; }

    /// <summary>The participant mentioned at <see cref="Range"/>, if this run represents an @mention.</summary>
    public ParticipantId? Mention { get; init; }
}
