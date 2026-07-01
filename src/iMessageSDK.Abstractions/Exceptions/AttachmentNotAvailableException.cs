namespace iMessageSDK.Exceptions;

/// <summary>
/// Thrown when an attachment's content is requested but is not currently available (see
/// <see cref="Attachments.Attachment.TransferState"/>).
/// </summary>
public sealed class AttachmentNotAvailableException : IMessageSdkException
{
    /// <summary>Initializes a new instance of the <see cref="AttachmentNotAvailableException"/> class.</summary>
    public AttachmentNotAvailableException()
        : base("The attachment's content is not currently available.")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AttachmentNotAvailableException"/> class with the given message.</summary>
    public AttachmentNotAvailableException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AttachmentNotAvailableException"/> class with the given message and inner exception.</summary>
    public AttachmentNotAvailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
