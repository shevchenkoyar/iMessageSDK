namespace iMessageSDK.Exceptions;

/// <summary>
/// Thrown when an operation fails because the Messages application is not installed or could not
/// be automated.
/// </summary>
public sealed class MessagesAppUnavailableException : IMessageSdkException
{
    /// <summary>Initializes a new instance of the <see cref="MessagesAppUnavailableException"/> class.</summary>
    public MessagesAppUnavailableException()
        : base("The Messages application is not available.")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MessagesAppUnavailableException"/> class with the given message.</summary>
    public MessagesAppUnavailableException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MessagesAppUnavailableException"/> class with the given message and inner exception.</summary>
    public MessagesAppUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
