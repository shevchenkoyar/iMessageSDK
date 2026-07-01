namespace iMessageSDK.Exceptions;

/// <summary>
/// Thrown when sending a message fails for a reason other than a missing permission (for
/// example, the target chat could not be found, or Messages reported an automation error).
/// </summary>
public sealed class MessageSendException : IMessageSdkException
{
    /// <summary>Initializes a new instance of the <see cref="MessageSendException"/> class.</summary>
    public MessageSendException()
        : base("The message could not be sent.")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MessageSendException"/> class with the given message.</summary>
    public MessageSendException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MessageSendException"/> class with the given message and inner exception.</summary>
    public MessageSendException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
