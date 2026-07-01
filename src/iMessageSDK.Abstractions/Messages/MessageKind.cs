namespace iMessageSDK.Messages;

/// <summary>
/// Categorizes the primary nature of a <see cref="Message"/> for filtering purposes.
/// </summary>
public enum MessageKind
{
    /// <summary>A message whose primary content is text. It may still carry attachments.</summary>
    PlainText,

    /// <summary>A message whose primary content is one or more attachments, with no text.</summary>
    Attachment,

    /// <summary>
    /// A system-generated event, such as a participant being added or removed, or a group chat
    /// being renamed. <see cref="Message.GroupActionDescription"/> carries a human-readable
    /// description.
    /// </summary>
    GroupAction,
}
