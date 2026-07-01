namespace iMessageSDK.Exceptions;

/// <summary>
/// Thrown when an operation fails because the process does not have permission to automate the
/// Messages application, which is required to send messages.
/// </summary>
/// <remarks>
/// Grant Automation access for the host process to control "Messages" under System Settings
/// &gt; Privacy &amp; Security &gt; Automation. See <see cref="Diagnostics.IDiagnosticsModule"/>
/// to check this programmatically before attempting an operation.
/// </remarks>
public sealed class AutomationPermissionDeniedException : IMessageSdkException
{
    /// <summary>Initializes a new instance of the <see cref="AutomationPermissionDeniedException"/> class.</summary>
    public AutomationPermissionDeniedException()
        : base("Automation access to control \"Messages\" has not been granted to this process. Grant it under System Settings > Privacy & Security > Automation.")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AutomationPermissionDeniedException"/> class with the given message.</summary>
    public AutomationPermissionDeniedException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AutomationPermissionDeniedException"/> class with the given message and inner exception.</summary>
    public AutomationPermissionDeniedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
