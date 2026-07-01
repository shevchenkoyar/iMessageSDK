namespace iMessageSDK.Exceptions;

/// <summary>
/// The base type for exceptions specific to iMessageSDK.
/// </summary>
public abstract class IMessageSdkException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="IMessageSdkException"/> class.</summary>
    protected IMessageSdkException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="IMessageSdkException"/> class with the given message.</summary>
    protected IMessageSdkException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="IMessageSdkException"/> class with the given message and inner exception.</summary>
    protected IMessageSdkException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
