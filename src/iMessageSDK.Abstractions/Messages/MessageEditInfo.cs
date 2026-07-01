namespace iMessageSDK.Messages;

/// <summary>
/// Describes the edit history of a <see cref="Message"/> whose text has been changed after sending.
/// </summary>
/// <remarks>
/// Full multi-revision history is recovered on a best-effort basis; at minimum
/// <see cref="LastEditedAt"/> and the current text (already reflected in <see cref="Message.Text"/>)
/// are always accurate.
/// </remarks>
public sealed record MessageEditInfo
{
    /// <summary>The moment the message was most recently edited.</summary>
    public required DateTimeOffset LastEditedAt { get; init; }

    /// <summary>The recovered revision history, oldest first. Contains at least one entry.</summary>
    public required IReadOnlyList<MessageEditEntry> History { get; init; }
}
