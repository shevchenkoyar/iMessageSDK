namespace iMessageSDK.Messages;

/// <summary>
/// The delivery status of a <see cref="Message"/>.
/// </summary>
public enum MessageStatus
{
    /// <summary>The message has not yet finished sending.</summary>
    Pending,

    /// <summary>The message was accepted for delivery.</summary>
    Sent,

    /// <summary>The message was delivered to at least one recipient's device.</summary>
    Delivered,

    /// <summary>At least one recipient has read the message.</summary>
    Read,

    /// <summary>The message failed to send.</summary>
    Failed,
}
