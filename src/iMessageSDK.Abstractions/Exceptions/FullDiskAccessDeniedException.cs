namespace iMessageSDK.Exceptions;

/// <summary>
/// Thrown when an operation fails because the process does not have Full Disk Access, which is
/// required to read Messages conversation history.
/// </summary>
/// <remarks>
/// Grant Full Disk Access to the host process under System Settings &gt; Privacy &amp; Security
/// &gt; Full Disk Access. See <see cref="Diagnostics.IDiagnosticsModule"/> to check this
/// programmatically before attempting an operation.
/// </remarks>
public sealed class FullDiskAccessDeniedException : IMessageSdkException
{
    /// <summary>Initializes a new instance of the <see cref="FullDiskAccessDeniedException"/> class.</summary>
    public FullDiskAccessDeniedException()
        : base("Full Disk Access has not been granted to this process. Grant it under System Settings > Privacy & Security > Full Disk Access.")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FullDiskAccessDeniedException"/> class with the given message.</summary>
    public FullDiskAccessDeniedException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FullDiskAccessDeniedException"/> class with the given message and inner exception.</summary>
    public FullDiskAccessDeniedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
