namespace iMessageSDK.Messages;

/// <summary>
/// The content to send as a new message: text, one or more attachments, or both.
/// </summary>
public sealed record MessageContent
{
    /// <summary>The text to send, if any.</summary>
    public string? Text { get; init; }

    /// <summary>The attachments to send, if any.</summary>
    public IReadOnlyList<OutgoingAttachment> Attachments { get; init; } = [];

    /// <summary>Creates a text-only <see cref="MessageContent"/>.</summary>
    /// <param name="text">The text to send.</param>
    public static MessageContent FromText(string text) => new() { Text = text };

    /// <summary>Creates a <see cref="MessageContent"/> carrying a single attachment.</summary>
    /// <param name="attachment">The attachment to send.</param>
    /// <param name="text">Optional accompanying text.</param>
    public static MessageContent FromAttachment(OutgoingAttachment attachment, string? text = null) =>
        new() { Text = text, Attachments = [attachment] };

    /// <summary>Creates a <see cref="MessageContent"/> carrying multiple attachments.</summary>
    /// <param name="attachments">The attachments to send.</param>
    /// <param name="text">Optional accompanying text.</param>
    public static MessageContent FromAttachments(IEnumerable<OutgoingAttachment> attachments, string? text = null) =>
        new() { Text = text, Attachments = attachments.ToList() };
}
