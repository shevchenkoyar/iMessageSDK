namespace iMessageSDK.Messages;

/// <summary>
/// Identifies the message a <see cref="Message"/> was sent in reply to (an inline/threaded reply).
/// </summary>
public sealed record ReplyMetadata
{
    /// <summary>The identifier of the message being replied to.</summary>
    public required MessageId RepliedToMessageId { get; init; }

    /// <summary>
    /// A short preview of the replied-to message's text, if it was available at mapping time.
    /// </summary>
    public string? RepliedToPreviewText { get; init; }
}
