namespace iMessageSDK.Exceptions;

/// <summary>
/// Thrown when iMessageSDK is used on an operating system other than macOS, which is the only
/// platform the Messages application runs on.
/// </summary>
public sealed class UnsupportedPlatformException : IMessageSdkException
{
    /// <summary>Initializes a new instance of the <see cref="UnsupportedPlatformException"/> class.</summary>
    public UnsupportedPlatformException()
        : base("iMessageSDK requires macOS.")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="UnsupportedPlatformException"/> class with the given message.</summary>
    public UnsupportedPlatformException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="UnsupportedPlatformException"/> class with the given message and inner exception.</summary>
    public UnsupportedPlatformException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
