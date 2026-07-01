using iMessageSDK.Attachments;
using iMessageSDK.Chats;

namespace iMessageSDK.Messages;

/// <summary>
/// An individual message within a <see cref="Chat"/>, encompassing plain text, rich formatting,
/// attachments, delivery status, replies, reactions, edits, and deletions.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Message"/> is an immutable snapshot taken at query or event time. To observe
/// subsequent changes to a message you already have (a new reaction, an edit, a deletion),
/// subscribe to <see cref="Watching.IMessageWatcher"/> rather than mutating this instance.
/// </para>
/// <para>
/// Sending replies, reactions, edits, or deletions is intentionally not yet part of the public
/// API: reading this information is fully supported today, and the corresponding sending
/// operations are reserved to be added as new members of <see cref="IMessagesModule"/> in a
/// future minor version without modifying any existing member.
/// </para>
/// </remarks>
public sealed record Message
{
    /// <summary>The unique, stable identifier of this message.</summary>
    public required MessageId Id { get; init; }

    /// <summary>The identifier of the chat this message belongs to.</summary>
    public required ChatId ChatId { get; init; }

    /// <summary>The primary nature of this message, useful for filtering.</summary>
    public required MessageKind Kind { get; init; }

    /// <summary>
    /// The participant who sent this message, or <see langword="null"/> if the sender could not
    /// be determined (for example, some system-generated <see cref="MessageKind.GroupAction"/> entries).
    /// </summary>
    public Participant? Sender { get; init; }

    /// <summary>
    /// <see langword="true"/> if this message was sent from the local account rather than
    /// received from another participant.
    /// </summary>
    public required bool IsFromMe { get; init; }

    /// <summary>The plain-text body of the message, if any.</summary>
    public string? Text { get; init; }

    /// <summary>
    /// The rich-text representation of <see cref="Text"/>, recovering formatting, links, and
    /// mentions when available. <see langword="null"/> when no rich formatting could be recovered.
    /// </summary>
    public AttributedText? AttributedText { get; init; }

    /// <summary>The attachments carried by this message, if any.</summary>
    public IReadOnlyList<Attachment> Attachments { get; init; } = [];

    /// <summary>The moment this message was sent.</summary>
    public required DateTimeOffset SentAt { get; init; }

    /// <summary>The moment this message was delivered, if known.</summary>
    public DateTimeOffset? DeliveredAt { get; init; }

    /// <summary>The moment this message was read by its recipient, if known.</summary>
    public DateTimeOffset? ReadAt { get; init; }

    /// <summary>The current delivery status of this message.</summary>
    public required MessageStatus Status { get; init; }

    /// <summary>The network this message was, or will be, delivered over.</summary>
    public DeliveryChannel Service { get; init; } = DeliveryChannel.IMessage;

    /// <summary>The message this message was sent in reply to, if it is an inline reply.</summary>
    public ReplyMetadata? ReplyTo { get; init; }

    /// <summary>The tapback reactions applied to this message.</summary>
    public IReadOnlyList<Reaction> Reactions { get; init; } = [];

    /// <summary>Edit history, if this message's text has been changed after sending.</summary>
    public MessageEditInfo? EditInfo { get; init; }

    /// <summary>Deletion (unsend) information, if this message was unsent after sending.</summary>
    public MessageDeletionInfo? DeletionInfo { get; init; }

    /// <summary>
    /// A human-readable description of the event, populated when <see cref="Kind"/> is
    /// <see cref="MessageKind.GroupAction"/> (for example, "Alex added Jordan to the conversation").
    /// </summary>
    public string? GroupActionDescription { get; init; }
}
